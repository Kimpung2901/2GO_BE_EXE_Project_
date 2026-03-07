using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IEscrowService
{
    Task<EscrowContract> EnsureForOrderAsync(Order order, long? paymentId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> FundForOrderAsync(long orderId, long? paymentId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> ReleaseForOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> RefundForOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> ForfeitDepositForOrderAsync(long orderId, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> PayoutDepositForCompletedOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<bool> PayoutRemainingForCompletedOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task RetryFailedForfeitPayoutsAsync(CancellationToken cancellationToken = default);
    Task<bool> RetryForfeitPayoutAsync(long escrowId, CancellationToken cancellationToken = default);
}
