using _2GO_EXE_Project.BAL.DTOs.Admin;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminPayoutService
{
    Task<AdminPayoutListResponse> GetForfeitPayoutsAsync(string? status, long? sellerId, long? orderId, int skip, int take, CancellationToken cancellationToken = default);
    Task<bool> RetryForfeitPayoutAsync(long escrowId, CancellationToken cancellationToken = default);
}
