using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System.Security.Claims;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

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
            throw new InvalidOperationException("JWT Secret is not configured. Please check your appsettings.json.");
        }
        if (_jwtSettings.RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException("JWT RefreshTokenLifetimeDays must be greater than 0.");
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ArgumentException("Email or phone is required.");
        }
        if (!IsValidPassword(request.Password))
        {
            throw new InvalidOperationException("Password must be at least 8 characters and include at least 1 letter and 1 digit.");
        }

        var exists = await _uow.Users.Query()
            .AsNoTracking()
            .AnyAsync(u => u.Email == request.Email || u.Phone == request.Phone, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var hash = _passwordHasher.HashPassword(request.Password, out var salt);
        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = hash,
            Salt = salt,
            Role = UserRoles.User,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
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

        return new RegisterResponse(user.UserId, "Register success");
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await _uow.Users.Query()
            .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.Phone == request.Identifier, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Salt))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // FIX 3: Check user status before allowing login
        if (user.Status != "Active")
        {
            throw new UnauthorizedAccessException("Account is not active.");
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
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, accessToken, refreshToken, expiresAt);
    }

    public async Task<BasicResponse> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _uow.RefreshTokens.Query()
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token is null)
        {
            return new BasicResponse(false, "Refresh token is invalid.");
        }

        if (token.RevokedAt is not null)
        {
            return new BasicResponse(false, "Refresh token is already revoked.");
        }

        token.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(token);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Logged out.");
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _uow.RefreshTokens.Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token == null || token.RevokedAt != null || token.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = token.User;

        // FIX 3: Check user status when refreshing token
        if (user.Status != "Active")
        {
            throw new UnauthorizedAccessException("Account is not active.");
        }

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);

        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken;
        _uow.RefreshTokens.Update(token);

        await _uow.SaveChangesAsync(cancellationToken);
        return new AuthResponse(user.UserId, user.Email, user.Phone, accessToken, newRefreshToken, expiresAt);
    }

    public async Task<BasicResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
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
            return new BasicResponse(false, "Code invalid or expired.");
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

        return new BasicResponse(true, "Email verified.");
    }

    public async Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new ArgumentException("IdToken is required.");
        }

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Firebase ID token");
            throw new UnauthorizedAccessException("Invalid Firebase token.");
        }

        decoded.Claims.TryGetValue("phone_number", out var phoneObj);
        var phone = phoneObj?.ToString();
        decoded.Claims.TryGetValue("email", out var emailObj);
        var email = emailObj?.ToString();
        var uid = decoded.Uid;

        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Firebase token missing phone/email.");
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
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        else
        {
            
            if (user.Status != "Active")
            {
                throw new UnauthorizedAccessException("Account is not active.");
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
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, accessToken, refreshToken, expiresAt);
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        // auto unban if expired
        if (user.Status == "Banned" && user.BanUntil != null && user.BanUntil <= DateTime.UtcNow)
        {
            user.Status = "Active";
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
                profile.AvatarUrl);

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
            profileInfo);
    }

    public async Task<UserInfoResponse> UpdateCurrentUserProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
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
        profile.Gender = request.Gender ?? profile.Gender;
        profile.AddressLine = request.Address ?? profile.AddressLine;
        profile.Bio = request.Bio ?? profile.Bio;
        profile.AvatarUrl = request.AvatarUrl ?? profile.AvatarUrl;

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
            profile.AvatarUrl);

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
            profileInfo);
    }

    public async Task<BasicResponse> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsValidPassword(request.NewPassword))
        {
            return new BasicResponse(false, "Password must be at least 8 characters and include at least 1 letter and 1 digit.");
        }
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var user = await _uow.Users.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.Salt))
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt))
        {
            return new BasicResponse(false, "Current password is incorrect.");
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

        return new BasicResponse(true, "Password changed. Please login again.");
    }

    public async Task<IReadOnlyList<DeviceResponse>> GetMyDevicesAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
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
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var device = await _uow.UserDevices.Query()
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId, cancellationToken);

        if (device == null)
        {
            return new BasicResponse(false, "Device not found.");
        }

        _uow.UserDevices.Remove(device);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Device removed.");
    }

    public async Task<IReadOnlyList<ActivityResponse>> GetMyActivityAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var logs = await _uow.ActivityLogs.Query()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ActivityResponse(l.LogId, l.Action, l.Details, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return logs;
    }

    public async Task<BasicResponse> UpdateAvatarAsync(ClaimsPrincipal userPrincipal, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
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
        return new BasicResponse(true, "Avatar updated.");
    }

    public async Task<BasicResponse> UpdateAddressAsync(ClaimsPrincipal userPrincipal, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var sub = userPrincipal.FindFirst("sub")?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? userPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (!long.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
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
        return new BasicResponse(true, "Address updated.");
    }

    public async Task<BasicResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(true, "If the email exists, a code has been sent."); // do not reveal existence
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
        return new BasicResponse(true, "If the email exists, a code has been sent.");
    }

    public async Task<BasicResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(false, "Invalid code.");
        }
        if (!IsValidPassword(request.NewPassword))
        {
            return new BasicResponse(false, "Password must be at least 8 characters and include at least 1 letter and 1 digit.");
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
            return new BasicResponse(false, "Code invalid or expired.");
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

        return new BasicResponse(true, "Password reset successful.");
    }

    private static bool IsValidPassword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8) return false;
        var hasLetter = value.Any(char.IsLetter);
        var hasDigit = value.Any(char.IsDigit);
        return hasLetter && hasDigit;
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
}
