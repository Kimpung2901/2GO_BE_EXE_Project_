using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Reports;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IReportService
{
    Task<ReportResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateReportRequest request, CancellationToken cancellationToken = default);
    Task<ReportListResponse> GetMyReportsAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default);
    Task<BasicResponse> ReplyAsync(ClaimsPrincipal userPrincipal, long reportId, ReplyReportRequest request, CancellationToken cancellationToken = default);
}
