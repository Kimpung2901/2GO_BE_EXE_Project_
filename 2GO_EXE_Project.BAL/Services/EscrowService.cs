using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Transfers;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace _2GO_EXE_Project.BAL.Services;

public class EscrowService : IEscrowService
{
    private readonly IUnitOfWork _uow;
    private readonly ITransferService _transferService;
    private readonly ILogger<EscrowService> _logger;

    public EscrowService(IUnitOfWork uow, ITransferService transferService, ILogger<EscrowService> logger)
    {
        _uow = uow;
        _transferService = transferService;
        _logger = logger;
    }

    private static bool RequiresDeposit(decimal totalAmount)
    {
        return totalAmount >= EscrowRules.DepositThresholdAmount;
    }

    private static decimal CalculateDepositAmount(decimal totalAmount)
    {
        if (!RequiresDeposit(totalAmount))
        {
            return 0m;
        }
        var amount = totalAmount * EscrowRules.DepositRate;
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<EscrowContract> EnsureForOrderAsync(Order order, long? paymentId, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
        if (existing != null)
        {
            if (paymentId.HasValue && existing.PaymentId != paymentId)
            {
                existing.PaymentId = paymentId;
                existing.UpdatedAt = DateTime.UtcNow;
                _uow.EscrowContracts.Update(existing);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            if (!existing.DepositRate.HasValue || existing.DepositRate.Value <= 0 || (existing.DepositAmount ?? 0m) <= 0m)
            {
                var existingTotal = existing.TotalAmount ?? order.TotalAmount ?? 0m;
                var requiresDepositExisting = RequiresDeposit(existingTotal);
                existing.DepositRate = requiresDepositExisting ? EscrowRules.DepositRate : 0m;
                existing.DepositAmount = CalculateDepositAmount(existingTotal);
                existing.UpdatedAt = DateTime.UtcNow;
                _uow.EscrowContracts.Update(existing);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        var totalAmount = order.TotalAmount ?? 0m;
        var depositAmount = CalculateDepositAmount(totalAmount);
        var requiresDeposit = RequiresDeposit(totalAmount);
        var escrow = new EscrowContract
        {
            BuyerId = order.BuyerId,
            SellerId = order.SellerId,
            ListingId = order.ListingId,
            OrderId = order.OrderId,
            PaymentId = paymentId,
            DepositAmount = depositAmount,
            TotalAmount = order.TotalAmount,
            DepositRate = requiresDeposit ? EscrowRules.DepositRate : 0m,
            Status = EscrowStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.EscrowContracts.AddAsync(escrow, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return escrow;
    }

    public async Task<EscrowContract?> FundForOrderAsync(long orderId, long? paymentId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null)
        {
            var order = await _uow.Orders.GetByIdAsync(orderId);
            if (order == null) return null;
            escrow = await EnsureForOrderAsync(order, paymentId, cancellationToken);
        }

        if (paymentId.HasValue && escrow.PaymentId != paymentId)
        {
            escrow.PaymentId = paymentId;
        }

        if (string.Equals(escrow.Status, EscrowStatuses.Funded, StringComparison.OrdinalIgnoreCase))
        {
            return escrow;
        }

        escrow.Status = EscrowStatuses.Funded;
        if (!escrow.DepositDeadlineAt.HasValue && (escrow.DepositAmount ?? 0m) > 0m)
        {
            escrow.DepositDeadlineAt = DateTime.UtcNow.AddHours(EscrowRules.DepositHoldHours);
        }
        escrow.UpdatedAt = DateTime.UtcNow;
        _uow.EscrowContracts.Update(escrow);
        await _uow.SaveChangesAsync(cancellationToken);
        return escrow;
    }

    public async Task<EscrowContract?> ReleaseForOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null) return null;
        if (string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase))
        {
            return escrow;
        }
        if (!string.Equals(escrow.Status, EscrowStatuses.Funded, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(escrow.Status, EscrowStatuses.Holding, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        escrow.Status = EscrowStatuses.Released;
        escrow.UpdatedAt = DateTime.UtcNow;
        _uow.EscrowContracts.Update(escrow);
        await _uow.SaveChangesAsync(cancellationToken);
        return escrow;
    }

    public async Task<EscrowContract?> RefundForOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null) return null;
        if (string.Equals(escrow.Status, EscrowStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
        {
            return escrow;
        }
        if (string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        escrow.Status = EscrowStatuses.Refunded;
        if (!escrow.DepositRefundedAt.HasValue)
        {
            escrow.DepositRefundedAt = DateTime.UtcNow;
        }
        escrow.UpdatedAt = DateTime.UtcNow;
        _uow.EscrowContracts.Update(escrow);
        await _uow.SaveChangesAsync(cancellationToken);
        return escrow;
    }

    public async Task<EscrowContract?> ForfeitDepositForOrderAsync(long orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null) return null;
        if (string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase))
        {
            return escrow;
        }
        if (string.Equals(escrow.Status, EscrowStatuses.Refunded, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        escrow.Status = EscrowStatuses.Released;
        if (!escrow.DepositForfeitedAt.HasValue)
        {
            escrow.DepositForfeitedAt = DateTime.UtcNow;
        }
        escrow.UpdatedAt = DateTime.UtcNow;
        _uow.EscrowContracts.Update(escrow);
        await _uow.SaveChangesAsync(cancellationToken);

        await TryAutoPayoutForForfeitAsync(escrow, cancellationToken);

        return escrow;
    }

    public async Task<bool> PayoutDepositForCompletedOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null) return false;
        if (!string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!escrow.SellerId.HasValue) return false;
        var amount = escrow.DepositAmount ?? 0m;
        if (amount <= 0m) return false;

        var hasPaid = await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                           t.Type == "DEPOSIT_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
        if (hasPaid) return true;

        await TryAutoPayoutForDepositAsync(escrow, cancellationToken);

        return await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                           t.Type == "DEPOSIT_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
    }

    public async Task<bool> PayoutRemainingForCompletedOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
        if (escrow == null) return false;
        if (!string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!escrow.SellerId.HasValue) return false;

        var total = escrow.TotalAmount ?? 0m;
        var deposit = escrow.DepositAmount ?? 0m;
        var amount = Math.Max(total - deposit, 0m);
        if (amount <= 0m) return false;

        var hasPaid = await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                           t.Type == "REMAINING_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
        if (hasPaid) return true;

        await TryAutoPayoutForRemainingAsync(escrow, amount, cancellationToken);

        return await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                           t.Type == "REMAINING_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
    }

    public async Task RetryFailedForfeitPayoutsAsync(CancellationToken cancellationToken = default)
    {
        var failedEscrowIds = await _uow.EscrowTransactions.Query()
            .Where(t => t.EscrowId.HasValue &&
                        t.Type == "FORFEIT_PAYOUT" &&
                        t.Status == "FAILED")
            .Select(t => t.EscrowId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (failedEscrowIds.Count == 0) return;

        foreach (var escrowId in failedEscrowIds)
        {
            var escrow = await _uow.EscrowContracts.Query()
                .FirstOrDefaultAsync(e => e.EscrowId == escrowId, cancellationToken);
            if (escrow == null) continue;
            if (!string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase)) continue;
            if (!escrow.SellerId.HasValue) continue;

            var hasPaid = await _uow.EscrowTransactions.Query()
                .AnyAsync(t => t.EscrowId == escrowId &&
                               t.Type == "FORFEIT_PAYOUT" &&
                               t.Status == "PAID", cancellationToken);
            if (hasPaid) continue;

            var seller = await _uow.Users.Query()
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == escrow.SellerId.Value, cancellationToken);
            var profile = seller?.UserProfiles.OrderBy(p => p.ProfileId).FirstOrDefault();
            if (profile == null) continue;
            if (string.IsNullOrWhiteSpace(profile.BankBin) ||
                string.IsNullOrWhiteSpace(profile.BankAccountNumber))
            {
                continue;
            }

            var bankBin = profile.BankBin.Trim();
            var bankActive = await _uow.Banks.Query()
                .AnyAsync(b => b.Bin == bankBin && b.IsActive, cancellationToken);
            if (!bankActive) continue;

            await TryAutoPayoutForForfeitAsync(escrow, cancellationToken);
        }
    }

    public async Task<bool> RetryForfeitPayoutAsync(long escrowId, CancellationToken cancellationToken = default)
    {
        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.EscrowId == escrowId, cancellationToken);
        if (escrow == null) return false;
        if (!string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase)) return false;
        if (!escrow.SellerId.HasValue) return false;

        var hasPaid = await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrowId &&
                           t.Type == "FORFEIT_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
        if (hasPaid) return true;

        await TryAutoPayoutForForfeitAsync(escrow, cancellationToken);

        return await _uow.EscrowTransactions.Query()
            .AnyAsync(t => t.EscrowId == escrowId &&
                           t.Type == "FORFEIT_PAYOUT" &&
                           t.Status == "PAID", cancellationToken);
    }

    private async Task TryAutoPayoutForForfeitAsync(EscrowContract escrow, CancellationToken cancellationToken)
    {
        try
        {
            if (!escrow.SellerId.HasValue) return;
            var amount = escrow.DepositAmount ?? 0m;
            if (amount <= 0m) return;

            var existingPaid = await _uow.EscrowTransactions.Query()
                .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                               t.Type == "FORFEIT_PAYOUT" &&
                               t.Status == "PAID", cancellationToken);
            if (existingPaid) return;

            var seller = await _uow.Users.Query()
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == escrow.SellerId.Value, cancellationToken);
            var profile = seller?.UserProfiles.OrderBy(p => p.ProfileId).FirstOrDefault();
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.BankBin) ||
                string.IsNullOrWhiteSpace(profile.BankAccountNumber))
            {
                _logger.LogWarning("Auto payout skipped: missing bank info for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "FORFEIT_PAYOUT", cancellationToken);
                return;
            }

            var bankBin = profile.BankBin.Trim();
            var bankActive = await _uow.Banks.Query()
                .AnyAsync(b => b.Bin == bankBin && b.IsActive, cancellationToken);
            if (!bankActive)
            {
                _logger.LogWarning("Auto payout skipped: bank bin not found or inactive for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "FORFEIT_PAYOUT", cancellationToken);
                return;
            }

            var payoutAmount = Convert.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
            if (payoutAmount <= 0) return;

            var payoutTx = await RecordPayoutTransactionAsync(escrow, amount, "PENDING", "FORFEIT_PAYOUT", cancellationToken);

            var transferRequest = new CreateTransferRequest(
                payoutAmount,
                $"Escrow deposit forfeited for order {escrow.OrderId}",
                bankBin,
                profile.BankAccountNumber.Trim(),
                new List<string> { "escrow_forfeit" });

            await _transferService.CreateSystemTransferAsync(escrow.SellerId.Value, transferRequest, cancellationToken);

            if (payoutTx != null)
            {
                payoutTx.Status = "PAID";
                _uow.EscrowTransactions.Update(payoutTx);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto payout failed for escrow {EscrowId}.", escrow.EscrowId);
            await RecordPayoutTransactionAsync(escrow, escrow.DepositAmount ?? 0m, "FAILED", "FORFEIT_PAYOUT", cancellationToken);
        }
    }

    private async Task TryAutoPayoutForDepositAsync(EscrowContract escrow, CancellationToken cancellationToken)
    {
        try
        {
            if (!escrow.SellerId.HasValue) return;
            var amount = escrow.DepositAmount ?? 0m;
            if (amount <= 0m) return;

            var existingPaid = await _uow.EscrowTransactions.Query()
                .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                               t.Type == "DEPOSIT_PAYOUT" &&
                               t.Status == "PAID", cancellationToken);
            if (existingPaid) return;

            var seller = await _uow.Users.Query()
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == escrow.SellerId.Value, cancellationToken);
            var profile = seller?.UserProfiles.OrderBy(p => p.ProfileId).FirstOrDefault();
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.BankBin) ||
                string.IsNullOrWhiteSpace(profile.BankAccountNumber))
            {
                _logger.LogWarning("Deposit payout skipped: missing bank info for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "DEPOSIT_PAYOUT", cancellationToken);
                return;
            }

            var bankBin = profile.BankBin.Trim();
            var bankActive = await _uow.Banks.Query()
                .AnyAsync(b => b.Bin == bankBin && b.IsActive, cancellationToken);
            if (!bankActive)
            {
                _logger.LogWarning("Deposit payout skipped: bank bin not found or inactive for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "DEPOSIT_PAYOUT", cancellationToken);
                return;
            }

            var payoutAmount = Convert.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
            if (payoutAmount <= 0) return;

            var payoutTx = await RecordPayoutTransactionAsync(escrow, amount, "PENDING", "DEPOSIT_PAYOUT", cancellationToken);

            var transferRequest = new CreateTransferRequest(
                payoutAmount,
                $"Escrow deposit released for order {escrow.OrderId}",
                bankBin,
                profile.BankAccountNumber.Trim(),
                new List<string> { "escrow_deposit_release" });

            await _transferService.CreateSystemTransferAsync(escrow.SellerId.Value, transferRequest, cancellationToken);

            if (payoutTx != null)
            {
                payoutTx.Status = "PAID";
                _uow.EscrowTransactions.Update(payoutTx);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deposit payout failed for escrow {EscrowId}.", escrow.EscrowId);
            await RecordPayoutTransactionAsync(escrow, escrow.DepositAmount ?? 0m, "FAILED", "DEPOSIT_PAYOUT", cancellationToken);
        }
    }

    private async Task TryAutoPayoutForRemainingAsync(EscrowContract escrow, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            if (!escrow.SellerId.HasValue) return;
            if (amount <= 0m) return;

            var existingPaid = await _uow.EscrowTransactions.Query()
                .AnyAsync(t => t.EscrowId == escrow.EscrowId &&
                               t.Type == "REMAINING_PAYOUT" &&
                               t.Status == "PAID", cancellationToken);
            if (existingPaid) return;

            var seller = await _uow.Users.Query()
                .Include(u => u.UserProfiles)
                .FirstOrDefaultAsync(u => u.UserId == escrow.SellerId.Value, cancellationToken);
            var profile = seller?.UserProfiles.OrderBy(p => p.ProfileId).FirstOrDefault();
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.BankBin) ||
                string.IsNullOrWhiteSpace(profile.BankAccountNumber))
            {
                _logger.LogWarning("Remaining payout skipped: missing bank info for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "REMAINING_PAYOUT", cancellationToken);
                return;
            }

            var bankBin = profile.BankBin.Trim();
            var bankActive = await _uow.Banks.Query()
                .AnyAsync(b => b.Bin == bankBin && b.IsActive, cancellationToken);
            if (!bankActive)
            {
                _logger.LogWarning("Remaining payout skipped: bank bin not found or inactive for seller {SellerId}.", escrow.SellerId);
                await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "REMAINING_PAYOUT", cancellationToken);
                return;
            }

            var payoutAmount = Convert.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
            if (payoutAmount <= 0) return;

            var payoutTx = await RecordPayoutTransactionAsync(escrow, amount, "PENDING", "REMAINING_PAYOUT", cancellationToken);

            var transferRequest = new CreateTransferRequest(
                payoutAmount,
                $"Escrow remaining payout for order {escrow.OrderId}",
                bankBin,
                profile.BankAccountNumber.Trim(),
                new List<string> { "escrow_remaining_payout" });

            await _transferService.CreateSystemTransferAsync(escrow.SellerId.Value, transferRequest, cancellationToken);

            if (payoutTx != null)
            {
                payoutTx.Status = "PAID";
                _uow.EscrowTransactions.Update(payoutTx);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remaining payout failed for escrow {EscrowId}.", escrow.EscrowId);
            await RecordPayoutTransactionAsync(escrow, amount, "FAILED", "REMAINING_PAYOUT", cancellationToken);
        }
    }

    private async Task<EscrowTransaction?> RecordPayoutTransactionAsync(
        EscrowContract escrow,
        decimal amount,
        string status,
        string type,
        CancellationToken cancellationToken)
    {
        try
        {
            var tx = new EscrowTransaction
            {
                EscrowId = escrow.EscrowId,
                Method = "PAYOS",
                Amount = amount,
                Type = type,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.EscrowTransactions.AddAsync(tx, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return tx;
        }
        catch
        {
            // ignore transaction logging failures
            return null;
        }
    }
}
