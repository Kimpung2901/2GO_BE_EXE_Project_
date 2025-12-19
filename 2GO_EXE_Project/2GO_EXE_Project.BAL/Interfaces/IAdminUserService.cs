using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(string? search, string? role, string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<AdminUserDetail?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<AdminUserDetail> UpdateUserAsync(long userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateUserRoleAsync(long userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateUserStatusAsync(long userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> DeleteUserAsync(long userId, CancellationToken cancellationToken = default); // soft delete
}
