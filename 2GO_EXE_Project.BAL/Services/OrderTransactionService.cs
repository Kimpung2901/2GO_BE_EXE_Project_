using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class OrderTransactionService : IOrderTransactionService
{
    private readonly IUnitOfWork _uow;

    public OrderTransactionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<OrderTransaction> CreateTransactionAsync(OrderTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        await _uow.OrderTransactions.AddAsync(transaction, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return transaction;
    }

    public async Task<List<OrderTransaction>> GetTransactionsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _uow.OrderTransactions.Query()
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.TransactionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderTransaction?> GetTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return await _uow.OrderTransactions.Query()
            .FirstOrDefaultAsync(t => t.Reference == reference, cancellationToken);
    }

    public async Task<bool> TransactionExistsAsync(long orderId, string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        return await _uow.OrderTransactions.Query()
            .AnyAsync(t => t.OrderId == orderId && t.Reference == reference, cancellationToken);
    }
}
