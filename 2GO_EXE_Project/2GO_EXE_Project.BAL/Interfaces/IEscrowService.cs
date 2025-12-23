using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IEscrowService
{
    Task<EscrowContract> EnsureForOrderAsync(Order order, long? paymentId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> FundForOrderAsync(long orderId, long? paymentId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> ReleaseForOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<EscrowContract?> RefundForOrderAsync(long orderId, CancellationToken cancellationToken = default);
}
