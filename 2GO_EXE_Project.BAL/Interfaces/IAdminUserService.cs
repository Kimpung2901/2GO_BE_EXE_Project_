using _2GO_EXE_Project.BAL.DTOs.Auth;
using System.Security.Claims;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminUserService
{
    Task<AdminUserListResponse> GetUsersAsync(string? search, string? role, string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<AdminUserDetail?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<AdminUserDetail> CreateUserAsync(ClaimsPrincipal adminPrincipal, AdminCreateUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDetail> UpdateUserAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateUserRoleAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateUserStatusAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> DeleteUserAsync(ClaimsPrincipal adminPrincipal, long userId, CancellationToken cancellationToken = default); // soft delete
    Task<IReadOnlyList<ActivityResponse>> GetAuditLogsAsync(int skip, int take, CancellationToken cancellationToken = default);
}
