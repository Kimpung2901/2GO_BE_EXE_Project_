using _2GO_EXE_Project.BAL.DTOs.Admin;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<AdminTimeseriesResponse> GetTimeseriesAsync(DateTime? from, DateTime? to, string bucket, CancellationToken cancellationToken = default);
}
