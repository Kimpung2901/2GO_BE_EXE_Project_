using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IModeratorService
{
    Task<AdminUserListResponse> GetUsersAsync(string? search, string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<BasicResponse> BanUserAsync(ClaimsPrincipal modPrincipal, long userId, BanUserRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UnbanUserAsync(ClaimsPrincipal modPrincipal, long userId, CancellationToken cancellationToken = default);

    Task<ModeratorReportListResponse> GetReportsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<ReportDetail?> GetReportByIdAsync(long reportId, CancellationToken cancellationToken = default);
    Task<BasicResponse> ResolveReportAsync(ClaimsPrincipal modPrincipal, long reportId, ResolveReportRequest request, CancellationToken cancellationToken = default);
}
