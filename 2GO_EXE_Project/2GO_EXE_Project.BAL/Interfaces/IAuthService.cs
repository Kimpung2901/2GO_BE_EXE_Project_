using _2GO_EXE_Project.BAL.DTOs.Auth;
using System.Security.Claims;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> FirebaseLoginAsync(FirebaseLoginRequest request, CancellationToken cancellationToken = default);
    Task<UserInfoResponse> GetCurrentUserAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<UserInfoResponse> UpdateCurrentUserProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> ChangePasswordAsync(ClaimsPrincipal userPrincipal, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceResponse>> GetMyDevicesAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<BasicResponse> RemoveMyDeviceAsync(ClaimsPrincipal userPrincipal, long deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityResponse>> GetMyActivityAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateAvatarAsync(ClaimsPrincipal userPrincipal, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateAddressAsync(ClaimsPrincipal userPrincipal, UpdateAddressRequest request, CancellationToken cancellationToken = default);
}
