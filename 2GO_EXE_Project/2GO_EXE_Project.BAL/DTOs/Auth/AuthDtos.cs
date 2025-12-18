namespace _2GO_EXE_Project.BAL.DTOs.Auth;

public record RegisterRequest(string? Email, string? Phone, string Password, string? FullName);
public record LoginRequest(string Identifier, string Password); // Identifier = email or phone
public record RefreshTokenRequest(string RefreshToken);
public record VerifyEmailRequest(string Email, string Code);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record RegisterResponse(long UserId, string Message);
public record AuthResponse(long UserId, string? Email, string? Phone, string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
public record BasicResponse(bool Success, string Message);
public record FirebaseLoginRequest(string IdToken);
public record UserInfoResponse(long UserId, string? Email, string? Phone, string? Role, string? Status, DateTime? CreatedAt, DateTime? LastLoginAt, bool EmailVerified, bool PhoneVerified);
