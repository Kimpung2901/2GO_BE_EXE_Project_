using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Escrow;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class EscrowService : IEscrowService
{
    private readonly IUnitOfWork _uow;

    public EscrowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }
        return id;
    }

    public async Task<EscrowResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateEscrowRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .Include(o => o.Listing)
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.BuyerId != userId) throw new InvalidOperationException("Only buyer can create escrow.");

        var existing = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.Orders.Any(o => o.OrderId == order.OrderId), cancellationToken);
        if (existing != null)
        {
            return new EscrowResponse(existing.EscrowId, order.OrderId, existing.BuyerId ?? 0, existing.SellerId ?? 0, existing.DepositAmount, existing.TotalAmount, existing.Status, existing.CreatedAt);
        }

        var escrow = new EscrowContract
        {
            BuyerId = order.BuyerId,
            SellerId = order.SellerId,
            ListingId = order.ListingId,
            DepositAmount = request.DepositAmount,
            TotalAmount = order.TotalAmount,
            Status = EscrowStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.EscrowContracts.AddAsync(escrow, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        order.EscrowId = escrow.EscrowId;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EscrowResponse(escrow.EscrowId, order.OrderId, escrow.BuyerId ?? 0, escrow.SellerId ?? 0, escrow.DepositAmount, escrow.TotalAmount, escrow.Status, escrow.CreatedAt);
    }

    public async Task<EscrowResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .Include(o => o.Escrow)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order == null || order.Escrow == null) return null;
        if (order.BuyerId != userId && order.SellerId != userId) return null;

        var escrow = order.Escrow;
        return new EscrowResponse(escrow.EscrowId, order.OrderId, escrow.BuyerId ?? 0, escrow.SellerId ?? 0, escrow.DepositAmount, escrow.TotalAmount, escrow.Status, escrow.CreatedAt);
    }

    public async Task<EscrowTransactionResponse> AddTransactionAsync(ClaimsPrincipal userPrincipal, long escrowId, CreateEscrowTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var escrow = await _uow.EscrowContracts.GetByIdAsync(escrowId);
        if (escrow == null) throw new InvalidOperationException("Escrow not found.");
        if (escrow.BuyerId != userId && escrow.SellerId != userId)
        {
            throw new InvalidOperationException("Not allowed.");
        }

        var tx = new EscrowTransaction
        {
            EscrowId = escrow.EscrowId,
            Method = request.Method,
            Amount = request.Amount,
            Type = request.Type,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _uow.EscrowTransactions.AddAsync(tx, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EscrowTransactionResponse(tx.TxId, tx.EscrowId ?? 0, tx.Type, tx.Method, tx.Amount, tx.Status, tx.CreatedAt);
    }

    public async Task<BasicResponse> ReleaseAsync(ClaimsPrincipal userPrincipal, long escrowId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var escrow = await _uow.EscrowContracts.GetByIdAsync(escrowId);
        if (escrow == null) return new BasicResponse(false, "Escrow not found.");
        if (escrow.BuyerId != userId) return new BasicResponse(false, "Only buyer can release.");

        escrow.Status = EscrowStatuses.Released;
        escrow.UpdatedAt = DateTime.UtcNow;
        _uow.EscrowContracts.Update(escrow);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Escrow released.");
    }
}
