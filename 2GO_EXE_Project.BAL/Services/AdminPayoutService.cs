using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Admin;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class AdminPayoutService : IAdminPayoutService
{
    private readonly IUnitOfWork _uow;
    private readonly IEscrowService _escrowService;

    public AdminPayoutService(IUnitOfWork uow, IEscrowService escrowService)
    {
        _uow = uow;
        _escrowService = escrowService;
    }

    public async Task<AdminPayoutListResponse> GetForfeitPayoutsAsync(string? status, long? sellerId, long? orderId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.EscrowTransactions.Query()
            .Include(t => t.Escrow)
                .ThenInclude(e => e.Seller)
                    .ThenInclude(u => u!.UserProfiles)
            .Where(t => t.Type == "FORFEIT_PAYOUT");

        if (!string.IsNullOrWhiteSpace(status))
        {
            var norm = status.Trim();
            query = query.Where(t => t.Status == norm);
        }

        if (sellerId.HasValue)
        {
            query = query.Where(t => t.Escrow != null && t.Escrow.SellerId == sellerId.Value);
        }

        if (orderId.HasValue)
        {
            query = query.Where(t => t.Escrow != null && t.Escrow.OrderId == orderId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(t => new AdminPayoutItem(
                t.EscrowId ?? 0,
                t.Escrow != null ? t.Escrow.OrderId : null,
                t.Escrow != null ? t.Escrow.SellerId : null,
                t.Escrow != null
                    ? t.Escrow.Seller!.UserProfiles.OrderBy(p => p.ProfileId).Select(p => p.FullName).FirstOrDefault()
                    : null,
                t.Amount,
                t.Status ?? "UNKNOWN",
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminPayoutListResponse(total, items);
    }

    public async Task<bool> RetryForfeitPayoutAsync(long escrowId, CancellationToken cancellationToken = default)
    {
        await _escrowService.RetryFailedForfeitPayoutsAsync(cancellationToken);
        var hasPaid = await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrowId && t.Type == "FORFEIT_PAYOUT" && t.Status == "PAID", cancellationToken);
        return hasPaid;
    }
}
