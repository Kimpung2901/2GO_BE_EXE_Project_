using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IOrderTransactionService
{
    Task<OrderTransaction> CreateTransactionAsync(OrderTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<OrderTransaction>> GetTransactionsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<OrderTransaction?> GetTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<bool> TransactionExistsAsync(long orderId, string reference, CancellationToken cancellationToken = default);
}
