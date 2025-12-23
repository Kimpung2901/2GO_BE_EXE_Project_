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
public record UserProfileInfo(string? FullName, DateOnly? Birthday, string? Gender, string? Address, string? Bio, string? AvatarUrl);
public record UserInfoResponse(
    long UserId,
    string? Email,
    string? Phone,
    string? Role,
    string? Status,
    DateTime? CreatedAt,
    DateTime? LastLoginAt,
    bool EmailVerified,
    bool PhoneVerified,
    UserProfileInfo? Profile);

public record UpdateProfileRequest(string? FullName, DateOnly? Birthday, string? Gender, string? Address, string? Bio, string? AvatarUrl);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record DeviceResponse(long DeviceId, string? DeviceInfo, string? IpAddress, DateTime? LastActive);
public record ActivityResponse(long LogId, string? Action, string? Details, DateTime? CreatedAt);
public record AdminUserSummary(long UserId, string? Email, string? Phone, string? Role, string? Status, DateTime? CreatedAt, DateTime? LastLoginAt, bool EmailVerified, bool PhoneVerified, string? FullName);
public record AdminUserDetail(
    long UserId,
    string? Email,
    string? Phone,
    string? Role,
    string? Status,
    DateTime? BanUntil,
    DateTime? CreatedAt,
    DateTime? LastLoginAt,
    bool EmailVerified,
    bool PhoneVerified,
    UserProfileInfo? Profile);
public record UpdateUserRequest(string? Email, string? Phone, string? Status, string? FullName, DateOnly? Birthday, string? Gender, string? Address, string? Bio, string? AvatarUrl);
public record UpdateUserRoleRequest(string Role);
public record UpdateUserStatusRequest(string Status);
public record AdminCreateUserRequest(string? Email, string? Phone, string? Password, string Role, string Status, string? FullName, DateOnly? Birthday, string? Gender, string? Address, string? Bio, string? AvatarUrl);
public record AdminUserListResponse(int Total, IReadOnlyList<AdminUserSummary> Items);
public record UpdateAvatarRequest(string AvatarUrl);
public record UpdateAddressRequest(string? Address, int? CityId, int? DistrictId, int? WardId);
public record BanUserRequest(string Reason, int? DurationDays);
public record ReportSummary(long ReportId, long? OrderId, long? ReporterId, long? TargetUserId, long? ListingId, string? Reason, string? Status, long? WaitingForUserId, DateTime? CreatedAt);
public record ReportDetail(long ReportId, long? OrderId, long? ReporterId, long? TargetUserId, long? ListingId, string? Reason, string? Status, long? WaitingForUserId, DateTime? CreatedAt);
public record ModeratorReportListResponse(int Total, IReadOnlyList<ReportSummary> Items);
public record ResolveReportRequest(string? Status, string? WaitingForRole, string? Decision, string? Note);
