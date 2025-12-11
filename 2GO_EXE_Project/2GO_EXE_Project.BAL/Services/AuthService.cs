using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ArgumentException("Email or phone is required.");
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
            Role = "User",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var code = await CreateVerificationCodeAsync(user.UserId, "EmailVerify", cancellationToken);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _emailService.SendAsync(user.Email, "Verify your email", $"Your verification code is: {code}", cancellationToken);
        }

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, accessToken, refreshToken, expiresAt);
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

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.UserId, user.Email, user.Phone, accessToken, refreshToken, expiresAt);
    }

    public async Task<BasicResponse> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _uow.RefreshTokens.Query()
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token != null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            _uow.RefreshTokens.Update(token);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return new BasicResponse(true, "Logged out");
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
        return new BasicResponse(true, "Email verified.");
    }

    public async Task<BasicResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            return new BasicResponse(true, "If the email exists, a code has been sent."); // do not reveal existence
        }

        var code = await CreateVerificationCodeAsync(user.UserId, "ForgotPassword", cancellationToken);
        await _emailService.SendAsync(request.Email, "Reset password", $"Your reset code is: {code}", cancellationToken);
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
        return new BasicResponse(true, "Password reset successful.");
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
}
