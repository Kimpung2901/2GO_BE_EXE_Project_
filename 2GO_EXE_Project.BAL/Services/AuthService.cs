using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System.Security.Claims;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Subscriptions;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private static readonly TimeZoneInfo VnTimeZone = ResolveVnTimeZone();

    public AuthService(
        IUnitOfWork uow,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;

        // FIX 1: Validate JWT Settings at construction time
        if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
        {
            throw new InvalidOperationException("JWT Secret chưa được cấu hình. Vui lòng kiểm tra appsettings.json.");
        }
        if (_jwtSettings.RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException("JWT RefreshTokenLifetimeDays phải lớn hơn 0.");
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateRegister(request));

        var exists = await _uow.Users.Query()
            .AsNoTracking()
            .AnyAsync(u => u.Email == request.Email || u.Phone == request.Phone, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Người dùng đã tồn tại.");
        }

        var hash = _passwordHasher.HashPassword(request.Password, out var salt);
        var now = DateTime.UtcNow;
        var freePlan = await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .Where(p => p.IsActive && p.Price <= 0)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PlanId)
            .FirstOrDefaultAsync(cancellationToken);
        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = hash,
            Salt = salt,
            Role = UserRoles.User,
            Status = UserStatuses.Active,
            CreatedAt = now,
            SubscriptionPlanCode = freePlan?.Code,
            SubscriptionValidFrom = freePlan == null ? null : now,
            SubscriptionValidUntil = freePlan == null || freePlan.DurationDays <= 0 ? null : now.AddDays(freePlan.DurationDays),
            SubscriptionUntil = freePlan == null || freePlan.DurationDays <= 0 ? null : now.AddDays(freePlan.DurationDays)
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var code = await CreateVerificationCodeAsync(user.UserId, "EmailVerify", cancellationToken);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                await _emailService.SendAsync(user.Email, "Verify your email", $"Your verification code is: {code}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(user.UserId, "Đăng ký thành công");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateLogin(request));
        var user = await _uow.Users.Query()
            .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.Phone == request.Identifier, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Salt))
        {
            throw new UnauthorizedAccessException("Thông tin đăng nhập không đúng.");
        }

        // FIX 3: Check user status before allowing login
        if (!string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Tài khoản không hoạt động.");
        }

        var normalizedRole = UserRoles.Normalize(user.Role);
        if (!string.Equals(user.Role, normalizedRole, StringComparison.Ordinal))
        {
            user.Role = normalizedRole;
            _uow.Users.Update(user);
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
        {
            throw new UnauthorizedAccessException("Password is incorrect.");
        }

        // FIX 2: Update LastLoginAt consistently
        user.LastLoginAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        var avatarUrl = await _uow.UserProfiles.Query()
            .Where(p => p.UserId == user.UserId)
            .Select(p => p.AvatarUrl)
            .FirstOrDefaultAsync(cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, user.Role, avatarUrl, accessToken, refreshToken, expiresAt);
    }

    public async Task<BasicResponse> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateRefreshToken(request));
        var token = await _uow.RefreshTokens.Query()
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token is null)
        {
            return new BasicResponse(false, "Refresh token không hợp lệ.");
        }

        if (token.RevokedAt is not null)
        {
            return new BasicResponse(false, "Refresh token đã bị thu hồi.");
        }

        token.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(token);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Đã đăng xuất.");
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateRefreshToken(request));
        var token = await _uow.RefreshTokens.Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token == null || token.RevokedAt != null || token.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        var user = token.User;

        // FIX 3: Check user status when refreshing token
        if (!string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Tài khoản không hoạt động.");
        }

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        var avatarUrl = await _uow.UserProfiles.Query()
            .Where(p => p.UserId == user.UserId)
            .Select(p => p.AvatarUrl)
            .FirstOrDefaultAsync(cancellationToken);

        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken;
        _uow.RefreshTokens.Update(token);

        await _uow.SaveChangesAsync(cancellationToken);
        return new AuthResponse(user.UserId, user.Email, user.Phone, user.Role, avatarUrl, accessToken, newRefreshToken, expiresAt);
    }

    public async Task<BasicResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateVerifyEmail(request));
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        var codeEntity = await _uow.VerificationCodes.Query()
            .FirstOrDefaultAsync(c =>
                c.UserId == user.UserId &&
                c.Code == request.Code &&
                c.Purpose == "EmailVerify" &&
                c.ConsumedAt == null &&
                c.ExpiresAt >= DateTime.UtcNow,
                cancellationToken);

        if (codeEntity == null)
        {
            return new BasicResponse(false, "Mã không hợp lệ hoặc đã hết hạn.");
        }

        codeEntity.ConsumedAt = DateTime.UtcNow;
        _uow.VerificationCodes.Update(codeEntity);

        var userVerify = await _uow.UserVerifications.Query()
            .FirstOrDefaultAsync(v => v.UserId == user.UserId, cancellationToken);

        if (userVerify == null)
        {
            userVerify = new UserVerification
            {
                UserId = user.UserId,
                EmailVerified = true,
                VerifiedAt = DateTime.UtcNow
            };
            await _uow.UserVerifications.AddAsync(userVerify, cancellationToken);
        }
        else
        {
            userVerify.EmailVerified = true;
            userVerify.VerifiedAt = DateTime.UtcNow;
            _uow.UserVerifications.Update(userVerify);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // FIX 4: Clean up expired verification codes
        await CleanupExpiredVerificationCodesAsync(user.UserId, cancellationToken);

        return new BasicResponse(true, "Đã xác thực email.");
    }

    public async Task<BasicResponse> ResendVerifyEmailAsync(ResendVerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateResendVerifyEmail(request));
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(true, "Nếu email tồn tại, mã đã được gửi.");
        }

        var userVerify = await _uow.UserVerifications.Query()
            .FirstOrDefaultAsync(v => v.UserId == user.UserId, cancellationToken);
        if (userVerify?.EmailVerified == true)
        {
            return new BasicResponse(true, "Email đã được xác thực.");
        }

        var code = await CreateVerificationCodeAsync(user.UserId, "EmailVerify", cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendAsync(user.Email, "Verify your email", $"Your verification code is: {code}", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Nếu email tồn tại, mã đã được gửi.");
    }

    public async Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateFirebaseLogin(request));

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Firebase ID token");
            throw new UnauthorizedAccessException("Firebase token không hợp lệ.");
        }

        decoded.Claims.TryGetValue("phone_number", out var phoneObj);
        var phone = phoneObj?.ToString();
        decoded.Claims.TryGetValue("email", out var emailObj);
        var email = emailObj?.ToString();
        var uid = decoded.Uid;

        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Firebase token thiếu phone/email.");
        }

        var user = await _uow.Users.Query()
            .FirstOrDefaultAsync(u => (!string.IsNullOrEmpty(phone) && u.Phone == phone) || (!string.IsNullOrEmpty(email) && u.Email == email), cancellationToken);

        if (user == null)
        {
            user = new User
            {
                Phone = phone,
                Email = email,
                Role = UserRoles.User,
            Status = UserStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        else
        {
            
            if (!string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Tài khoản không hoạt động.");
            }

            var normalizedRole = UserRoles.Normalize(user.Role);
            if (!string.Equals(user.Role, normalizedRole, StringComparison.Ordinal))
            {
                user.Role = normalizedRole;
                _uow.Users.Update(user);
            }
        }

        var userVerify = await _uow.UserVerifications.Query()
            .FirstOrDefaultAsync(v => v.UserId == user.UserId, cancellationToken);
        
        if (userVerify == null)
        {
            userVerify = new UserVerification
            {
                UserId = user.UserId,
                PhoneVerified = !string.IsNullOrWhiteSpace(phone),
                EmailVerified = !string.IsNullOrWhiteSpace(email),
                VerifiedAt = DateTime.UtcNow
            };
            await _uow.UserVerifications.AddAsync(userVerify, cancellationToken);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                userVerify.PhoneVerified = true;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                userVerify.EmailVerified = true;
            }
            userVerify.VerifiedAt = DateTime.UtcNow;
            _uow.UserVerifications.Update(userVerify);
        }

        user.LastLoginAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        var avatarUrl = await _uow.UserProfiles.Query()
            .Where(p => p.UserId == user.UserId)
            .Select(p => p.AvatarUrl)
            .FirstOrDefaultAsync(cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, user.Role, avatarUrl, accessToken, refreshToken, expiresAt);
    }

    public async Task<BasicResponse> VerifyPhoneFirebaseAsync(ClaimsPrincipal userPrincipal, VerifyPhoneFirebaseRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateVerifyPhoneFirebase(request));

        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Firebase ID token");
            throw new UnauthorizedAccessException("Firebase token không hợp lệ.");
        }

        decoded.Claims.TryGetValue("phone_number", out var phoneObj);
        var phone = phoneObj?.ToString();

        if (string.IsNullOrWhiteSpace(phone))
        {
            return new BasicResponse(false, "Firebase token thiếu phone_number.");
        }

        if (!string.IsNullOrWhiteSpace(user.Phone) &&
            !string.Equals(user.Phone, phone, StringComparison.Ordinal))
        {
            return new BasicResponse(false, "Số điện thoại không khớp với tài khoản hiện tại. Vui lòng dùng chức năng đổi số.");
        }

        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            var phoneInUse = await _uow.Users.Query()
                .AsNoTracking()
                .AnyAsync(u => u.UserId != user.UserId && u.Phone == phone, cancellationToken);

            if (phoneInUse)
            {
                return new BasicResponse(false, "Số điện thoại đã được sử dụng.");
            }

            user.Phone = phone;
            _uow.Users.Update(user);
        }

        var userVerify = user.UserVerifications
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefault();

        if (userVerify == null)
        {
            userVerify = new UserVerification
            {
                UserId = user.UserId,
                PhoneVerified = true,
                VerifiedAt = DateTime.UtcNow
            };
            await _uow.UserVerifications.AddAsync(userVerify, cancellationToken);
        }
        else
        {
            userVerify.PhoneVerified = true;
            userVerify.VerifiedAt = DateTime.UtcNow;
            _uow.UserVerifications.Update(userVerify);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Đã xác thực số điện thoại.");
    }

    public async Task<BasicResponse> ChangePhoneFirebaseAsync(ClaimsPrincipal userPrincipal, ChangePhoneFirebaseRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateChangePhoneFirebase(request));

        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Firebase ID token");
            throw new UnauthorizedAccessException("Firebase token không hợp lệ.");
        }

        decoded.Claims.TryGetValue("phone_number", out var phoneObj);
        var phone = phoneObj?.ToString();

        if (string.IsNullOrWhiteSpace(phone))
        {
            return new BasicResponse(false, "Firebase token thiếu phone_number.");
        }

        var phoneInUse = await _uow.Users.Query()
            .AsNoTracking()
            .AnyAsync(u => u.UserId != user.UserId && u.Phone == phone, cancellationToken);

        if (phoneInUse)
        {
            return new BasicResponse(false, "Số điện thoại đã được sử dụng.");
        }

        var phoneChanged = !string.Equals(user.Phone, phone, StringComparison.Ordinal);
        if (phoneChanged)
        {
            user.Phone = phone;
            _uow.Users.Update(user);
        }

        var userVerify = user.UserVerifications
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefault();

        if (userVerify == null)
        {
            userVerify = new UserVerification
            {
                UserId = user.UserId,
                PhoneVerified = true,
                VerifiedAt = DateTime.UtcNow
            };
            await _uow.UserVerifications.AddAsync(userVerify, cancellationToken);
        }
        else
        {
            userVerify.PhoneVerified = true;
            userVerify.VerifiedAt = DateTime.UtcNow;
            _uow.UserVerifications.Update(userVerify);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return phoneChanged
            ? new BasicResponse(true, "Số điện thoại đã được đổi và xác thực.")
            : new BasicResponse(true, "Đã xác thực số điện thoại.");
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        var now = DateTime.UtcNow;

        // auto unban if expired
        if (string.Equals(user.Status, UserStatuses.Banned, StringComparison.OrdinalIgnoreCase) &&
            user.BanUntil != null &&
            user.BanUntil <= now)
        {
            user.Status = UserStatuses.Active;
            user.BanUntil = null;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        var verification = user.UserVerifications.FirstOrDefault();
        var emailVerified = verification?.EmailVerified ?? false;
        var phoneVerified = verification?.PhoneVerified ?? false;
        var profile = user.UserProfiles.FirstOrDefault();
        var profileInfo = profile == null
            ? null
            : new UserProfileInfo(
                profile.FullName,
                profile.DateOfBirth,
                profile.Gender,
                profile.AddressLine,
                profile.Bio,
                profile.AvatarUrl,
                profile.BankAccountNumber,
                profile.BankBin,
                profile.BankAccountName);

        SubscriptionPlan? plan = null;
        var planCode = string.IsNullOrWhiteSpace(user.SubscriptionPlanCode)
            ? null
            : user.SubscriptionPlanCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(planCode))
        {
            plan = await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == planCode, cancellationToken);
        }
        plan ??= await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Price <= 0, cancellationToken);

        var subscriptionUntil = user.SubscriptionValidUntil ?? user.SubscriptionUntil;
        var subscriptionActive = subscriptionUntil.HasValue && subscriptionUntil.Value > now;
        int? remainingDays = null;
        if (subscriptionUntil.HasValue)
        {
            var remaining = subscriptionUntil.Value - now;
            remainingDays = remaining.TotalDays <= 0 ? 0 : (int)Math.Ceiling(remaining.TotalDays);
        }
        else if (plan?.Price <= 0)
        {
            subscriptionActive = true;
        }

        return new UserInfoResponse(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.LastLoginAt,
            emailVerified,
            phoneVerified,
            profileInfo,
            planCode ?? plan?.Code,
            plan?.Name,
            user.SubscriptionValidFrom,
            subscriptionUntil,
            subscriptionActive,
            remainingDays,
            plan?.MonthlyListingLimit);
    }

    public async Task<UserInfoResponse> UpdateCurrentUserProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateUpdateProfile(request));

        if (!string.IsNullOrWhiteSpace(request.BankBin))
        {
            var bankBin = request.BankBin.Trim();
            var bankActive = await _uow.Banks.Query()
                .AnyAsync(b => b.Bin == bankBin && b.IsActive, cancellationToken);
            if (!bankActive)
            {
                throw new InvalidOperationException("BankBin không hợp lệ hoặc không hoạt động.");
            }
        }

        var newPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(newPhone) &&
            !string.Equals(newPhone, user.Phone, StringComparison.Ordinal))
        {
            var phoneExists = await _uow.Users.Query()
                .AsNoTracking()
                .AnyAsync(u => u.UserId != user.UserId && u.Phone == newPhone, cancellationToken);
            if (phoneExists)
            {
                throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
            }

            user.Phone = newPhone;

            var phoneVerification = user.UserVerifications.FirstOrDefault();
            if (phoneVerification == null)
            {
                phoneVerification = new UserVerification
                {
                    UserId = user.UserId,
                    PhoneVerified = false,
                    EmailVerified = false
                };
                await _uow.UserVerifications.AddAsync(phoneVerification, cancellationToken);
            }
            else
            {
                phoneVerification.PhoneVerified = false;
                _uow.UserVerifications.Update(phoneVerification);
            }
        }

        var profile = user.UserProfiles.FirstOrDefault();
        var isNewProfile = profile == null;
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = user.UserId
            };
            await _uow.UserProfiles.AddAsync(profile, cancellationToken);
        }

        profile.FullName = request.FullName ?? profile.FullName;
        profile.DateOfBirth = request.Birthday ?? profile.DateOfBirth;
        profile.Gender = NormalizeGender(request.Gender) ?? profile.Gender;
        profile.AddressLine = request.Address ?? profile.AddressLine;
        profile.Bio = request.Bio ?? profile.Bio;
        profile.AvatarUrl = request.AvatarUrl ?? profile.AvatarUrl;
        profile.BankAccountNumber = request.BankAccountNumber ?? profile.BankAccountNumber;
        profile.BankBin = request.BankBin ?? profile.BankBin;
        profile.BankAccountName = request.BankAccountName ?? profile.BankAccountName;

        if (!isNewProfile)
        {
            _uow.UserProfiles.Update(profile);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var verification = user.UserVerifications.FirstOrDefault();
        var emailVerified = verification?.EmailVerified ?? false;
        var phoneVerified = verification?.PhoneVerified ?? false;

        var profileInfo = new UserProfileInfo(
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.AddressLine,
            profile.Bio,
            profile.AvatarUrl,
            profile.BankAccountNumber,
            profile.BankBin,
            profile.BankAccountName);

        var now = DateTime.UtcNow;
        SubscriptionPlan? plan = null;
        var planCode = string.IsNullOrWhiteSpace(user.SubscriptionPlanCode)
            ? null
            : user.SubscriptionPlanCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(planCode))
        {
            plan = await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == planCode, cancellationToken);
        }
        plan ??= await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Price <= 0, cancellationToken);

        var subscriptionUntil = user.SubscriptionValidUntil ?? user.SubscriptionUntil;
        var subscriptionActive = subscriptionUntil.HasValue && subscriptionUntil.Value > now;
        int? remainingDays = null;
        if (subscriptionUntil.HasValue)
        {
            var remaining = subscriptionUntil.Value - now;
            remainingDays = remaining.TotalDays <= 0 ? 0 : (int)Math.Ceiling(remaining.TotalDays);
        }
        else if (plan?.Price <= 0)
        {
            subscriptionActive = true;
        }

        return new UserInfoResponse(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.LastLoginAt,
            emailVerified,
            phoneVerified,
            profileInfo,
            planCode ?? plan?.Code,
            plan?.Name,
            user.SubscriptionValidFrom,
            subscriptionUntil,
            subscriptionActive,
            remainingDays,
            plan?.MonthlyListingLimit);
    }

    public async Task<BasicResponse> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateChangePassword(request));
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Salt))
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt))
        {
            return new BasicResponse(false, "Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword, out var newSalt);
        user.Salt = newSalt;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        // revoke all refresh tokens to force re-login
        var tokens = await _uow.RefreshTokens.Query()
            .Where(t => t.UserId == user.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }
        _uow.RefreshTokens.UpdateRange(tokens);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Mật khẩu đã được đổi. Vui lòng đăng nhập lại.");
    }

    public async Task<IReadOnlyList<DeviceResponse>> GetMyDevicesAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var devices = await _uow.UserDevices.Query()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastActive)
            .Select(d => new DeviceResponse(d.DeviceId, d.DeviceInfo, d.Ipaddress, d.LastActive))
            .ToListAsync(cancellationToken);

        return devices;
    }

    public async Task<BasicResponse> RemoveMyDeviceAsync(ClaimsPrincipal userPrincipal, long deviceId, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var device = await _uow.UserDevices.Query()
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId, cancellationToken);

        if (device == null)
        {
            return new BasicResponse(false, "Không tìm thấy thiết bị.");
        }

        _uow.UserDevices.Remove(device);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Đã xóa thiết bị.");
    }

    public async Task<IReadOnlyList<ActivityResponse>> GetMyActivityAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var logs = await _uow.ActivityLogs.Query()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ActivityResponse(l.LogId, l.Action, l.Details, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return logs;
    }

    public async Task<UserSubscriptionResponse> GetMySubscriptionAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        var now = DateTime.UtcNow;
        SubscriptionPlan? plan = null;
        var planCode = string.IsNullOrWhiteSpace(user.SubscriptionPlanCode)
            ? null
            : user.SubscriptionPlanCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(planCode))
        {
            plan = await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == planCode, cancellationToken);
        }
        plan ??= await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Price <= 0, cancellationToken);
        var until = user.SubscriptionUntil;
        var isActive = until.HasValue && until.Value > now;
        int? remainingDays = null;
        if (until.HasValue)
        {
            var remaining = until.Value - now;
            remainingDays = remaining.TotalDays <= 0 ? 0 : (int)Math.Ceiling(remaining.TotalDays);
        }
        else if (plan?.Price <= 0)
        {
            isActive = true;
        }

        return new UserSubscriptionResponse(
            user.UserId,
            planCode ?? plan?.Code,
            plan?.Name,
            until,
            isActive,
            remainingDays);
    }

    public async Task<UserSubscriptionUsageResponse> GetMySubscriptionUsageAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        var plan = await ResolveCurrentPlanAsync(userId, now, cancellationToken);
        var limit = plan?.MonthlyListingLimit;

        var usedCount = 0;
        if (plan != null)
        {
            var vnNow = TimeZoneInfo.ConvertTimeFromUtc(now, VnTimeZone);
            var monthStartVn = new DateTime(vnNow.Year, vnNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var nextMonthStartVn = monthStartVn.AddMonths(1);
            var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartVn, VnTimeZone);
            var nextMonthStartUtc = TimeZoneInfo.ConvertTimeToUtc(nextMonthStartVn, VnTimeZone);
            usedCount = await _uow.Listings.Query()
                .Where(l => l.SellerId == userId &&
                            l.PublishedAt.HasValue &&
                            l.PublishedAt.Value >= monthStartUtc &&
                            l.PublishedAt.Value < nextMonthStartUtc)
                .CountAsync(cancellationToken);
        }

        int? remaining = null;
        if (limit.HasValue)
        {
            remaining = Math.Max(0, limit.Value - usedCount);
        }

        return new UserSubscriptionUsageResponse(limit, usedCount, remaining);
    }

    private static TimeZoneInfo ResolveVnTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    public async Task<BasicResponse> UpdateAvatarAsync(ClaimsPrincipal userPrincipal, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateUpdateAvatar(request));
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        var profile = user.UserProfiles.FirstOrDefault();
        var isNew = profile == null;
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _uow.UserProfiles.AddAsync(profile, cancellationToken);
        }

        profile.AvatarUrl = request.AvatarUrl;
        if (!isNew)
        {
            _uow.UserProfiles.Update(profile);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Đã cập nhật ảnh đại diện.");
    }

    public async Task<BasicResponse> UpdateAddressAsync(ClaimsPrincipal userPrincipal, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateUpdateAddress(request));
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy người dùng.");
        }

        var profile = user.UserProfiles.FirstOrDefault();
        var isNew = profile == null;
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _uow.UserProfiles.AddAsync(profile, cancellationToken);
        }

        profile.AddressLine = request.Address ?? profile.AddressLine;
        profile.CityId = request.CityId ?? profile.CityId;
        profile.DistrictId = request.DistrictId ?? profile.DistrictId;
        profile.WardId = request.WardId ?? profile.WardId;

        if (!isNew)
        {
            _uow.UserProfiles.Update(profile);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Đã cập nhật địa chỉ.");
    }

    public async Task<BasicResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateForgotPassword(request));
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(true, "Nếu email tồn tại, mã đã được gửi."); // do not reveal existence
        }

        var code = await CreateVerificationCodeAsync(user.UserId, "ForgotPassword", cancellationToken);
        try
        {
            await _emailService.SendAsync(request.Email, "Reset password", $"Your reset code is: {code}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send forgot-password email to {Email}", request.Email);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Nếu email tồn tại, mã đã được gửi.");
    }

    public async Task<BasicResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateResetPassword(request));
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(false, "Mã không hợp lệ.");
        }

        var codeEntity = await _uow.VerificationCodes.Query()
            .FirstOrDefaultAsync(c =>
                c.UserId == user.UserId &&
                c.Code == request.Code &&
                c.Purpose == "ForgotPassword" &&
                c.ConsumedAt == null &&
                c.ExpiresAt >= DateTime.UtcNow,
                cancellationToken);

        if (codeEntity == null)
        {
            return new BasicResponse(false, "Mã không hợp lệ hoặc đã hết hạn.");
        }

        codeEntity.ConsumedAt = DateTime.UtcNow;
        _uow.VerificationCodes.Update(codeEntity);

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword, out var salt);
        user.Salt = salt;
        _uow.Users.Update(user);

        // revoke all refresh tokens for this user
        var tokens = await _uow.RefreshTokens.Query()
            .Where(t => t.UserId == user.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }
        _uow.RefreshTokens.UpdateRange(tokens);

        await _uow.SaveChangesAsync(cancellationToken);

        await CleanupExpiredVerificationCodesAsync(user.UserId, cancellationToken);

        return new BasicResponse(true, "Đặt lại mật khẩu thành công.");
    }

    private static string? NormalizeGender(string? gender)
    {
        if (gender == null) return null;
        var trimmed = gender.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private async Task<string> CreateVerificationCodeAsync(long userId, string purpose, CancellationToken cancellationToken)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var verification = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.VerificationCodes.AddAsync(verification, cancellationToken);
        return code;
    }

    private async Task<string> IssueRefreshTokenAsync(long userId, CancellationToken cancellationToken)
    {
        var token = _tokenService.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.RefreshTokens.AddAsync(refresh, cancellationToken);
        return token;
    }

    private async Task CleanupExpiredVerificationCodesAsync(long userId, CancellationToken cancellationToken)
    {
        var expiredCodes = await _uow.VerificationCodes.Query()
            .Where(c => c.UserId == userId && (c.ExpiresAt < DateTime.UtcNow || c.ConsumedAt != null))
            .ToListAsync(cancellationToken);

        if (expiredCodes.Any())
        {
            _uow.VerificationCodes.RemoveRange(expiredCodes);
        }
    }

    private async Task<SubscriptionPlan?> ResolveCurrentPlanAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        var userSub = await _uow.Users.Query()
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.SubscriptionPlanCode, u.SubscriptionValidUntil, u.SubscriptionUntil })
            .FirstOrDefaultAsync(cancellationToken);

        if (userSub != null)
        {
            var validUntil = userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil;
            if (validUntil.HasValue &&
                validUntil.Value > now &&
                !string.IsNullOrWhiteSpace(userSub.SubscriptionPlanCode))
            {
                var code = userSub.SubscriptionPlanCode.Trim().ToUpperInvariant();
                var planByCode = await _uow.SubscriptionPlans.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
                if (planByCode != null) return planByCode;
            }
        }

        var hasActiveSubscription = userSub != null &&
                                    (userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil).HasValue &&
                                    (userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil)!.Value > now;

        if (hasActiveSubscription)
        {
            var payment = await _uow.Payments.Query()
                .Where(p => p.UserId == userId &&
                            p.PaymentType == PaymentTypes.Subscription &&
                            p.Status == PaymentStatuses.Paid &&
                            p.SubscriptionValidUntil.HasValue &&
                            p.SubscriptionValidUntil.Value > now)
                .OrderByDescending(p => p.SubscriptionValidUntil)
                .FirstOrDefaultAsync(cancellationToken);

            var plan = await ResolvePlanForPaymentAsync(payment, cancellationToken);
            if (plan != null) return plan;
            return null;
        }

        var freePlan = await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Price <= 0, cancellationToken);
        return freePlan;
    }

    private async Task<SubscriptionPlan?> ResolvePlanForPaymentAsync(Payment? payment, CancellationToken cancellationToken)
    {
        if (payment == null) return null;

        if (!string.IsNullOrWhiteSpace(payment.SubscriptionPlanCode))
        {
            var code = payment.SubscriptionPlanCode.Trim().ToUpperInvariant();
            return await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
        }

        if (payment.Amount.HasValue && payment.SubscriptionDays.HasValue)
        {
            return await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.DurationDays == payment.SubscriptionDays.Value &&
                    p.Price == payment.Amount.Value, cancellationToken);
        }

        return null;
    }
}










