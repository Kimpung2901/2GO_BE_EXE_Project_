using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ModeratorService : IModeratorService
{
    private readonly IUnitOfWork _uow;

    public ModeratorService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private long? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    public async Task<AdminUserListResponse> GetUsersAsync(string? search, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)) ||
                u.UserProfiles.Any(p => p.FullName != null && p.FullName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(u => new AdminUserSummary(
                u.UserId,
                u.Email,
                u.Phone,
                u.Role,
                u.Status,
                u.CreatedAt,
                u.LastLoginAt,
                u.UserVerifications.FirstOrDefault().EmailVerified ?? false,
                u.UserVerifications.FirstOrDefault().PhoneVerified ?? false,
                u.UserProfiles.FirstOrDefault().FullName))
            .ToListAsync(cancellationToken);

        return new AdminUserListResponse(total, users);
    }

    public async Task<BasicResponse> BanUserAsync(ClaimsPrincipal modPrincipal, long userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = "Banned";
        if (request.DurationDays.HasValue)
        {
            user.BanUntil = DateTime.UtcNow.AddDays(request.DurationDays.Value);
        }
        _uow.Users.Update(user);

        var tokens = await _uow.RefreshTokens.Query()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }
        _uow.RefreshTokens.UpdateRange(tokens);

        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "BanUser", new { TargetUserId = userId, request.Reason, request.DurationDays }, cancellationToken);
        return new BasicResponse(true, "User banned.");
    }

    public async Task<BasicResponse> UnbanUserAsync(ClaimsPrincipal modPrincipal, long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = "Active";
        user.BanUntil = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "UnbanUser", new { TargetUserId = userId }, cancellationToken);
        return new BasicResponse(true, "User unbanned.");
    }

    public async Task<ModeratorReportListResponse> GetReportsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Reports.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(r => new ReportSummary(r.ReportId, r.OrderId, r.ReporterId, r.TargetUserId, r.ListingId, r.Reason, r.Status, r.WaitingForUserId, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ModeratorReportListResponse(total, items);
    }

    public async Task<ReportDetail?> GetReportByIdAsync(long reportId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Reports.Query()
            .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);
        if (report == null) return null;
        return new ReportDetail(report.ReportId, report.OrderId, report.ReporterId, report.TargetUserId, report.ListingId, report.Reason, report.Status, report.WaitingForUserId, report.CreatedAt);
    }

    public async Task<BasicResponse> ResolveReportAsync(ClaimsPrincipal modPrincipal, long reportId, ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            return new BasicResponse(false, "Report not found.");
        }

        var currentStatus = string.IsNullOrWhiteSpace(report.Status) ? ReportStatuses.Open : report.Status!;
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return new BasicResponse(false, "Status is required.");
        }

        var nextStatus = request.Status.Trim();
        if (!IsTransitionAllowed(currentStatus, nextStatus))
        {
            return new BasicResponse(false, $"Invalid report status transition: {currentStatus} -> {nextStatus}.");
        }

        if (string.Equals(nextStatus, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.WaitingForRole))
            {
                return new BasicResponse(false, "WaitingForRole is required.");
            }
            if (!report.OrderId.HasValue)
            {
                return new BasicResponse(false, "OrderId is required.");
            }

            var order = await _uow.Orders.GetByIdAsync(report.OrderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Order not found.");
            }

            var waitingUserId = ResolveWaitingUserId(order, request.WaitingForRole);
            if (!waitingUserId.HasValue)
            {
                return new BasicResponse(false, "Waiting user not found.");
            }

            report.WaitingForUserId = waitingUserId.Value;
        }
        else
        {
            report.WaitingForUserId = null;
        }

        if (string.Equals(nextStatus, ReportStatuses.Resolved, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Decision))
            {
                return new BasicResponse(false, "Decision is required for resolving reports.");
            }
            var orderId = report.OrderId;
            if (!orderId.HasValue)
            {
                return new BasicResponse(false, "OrderId is required.");
            }
            var order = await _uow.Orders.GetByIdAsync(orderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Order not found.");
            }
            await ApplyResolutionAsync(order, request.Decision, cancellationToken);
        }
        else if (string.Equals(nextStatus, ReportStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            var orderId = report.OrderId;
            if (!orderId.HasValue)
            {
                return new BasicResponse(false, "OrderId is required.");
            }
            var order = await _uow.Orders.GetByIdAsync(orderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Order not found.");
            }
            await ApplyRejectionAsync(order, cancellationToken);
        }

        report.Status = nextStatus;
        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "ResolveReport", new { ReportId = reportId, report.Status, request.WaitingForRole, request.Decision, request.Note }, cancellationToken);
        return new BasicResponse(true, "Report updated.");
    }

    private static bool IsTransitionAllowed(string current, string next)
    {
        if (string.Equals(current, ReportStatuses.Open, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ReportStatuses.InReview, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ReportStatuses.InReview, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ReportStatuses.Resolved, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ReportStatuses.Rejected, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ReportStatuses.Resolved, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ReportStatuses.Rejected, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static long? ResolveWaitingUserId(Order order, string waitingForRole)
    {
        if (string.Equals(waitingForRole, "Buyer", StringComparison.OrdinalIgnoreCase))
        {
            return order.BuyerId;
        }
        if (string.Equals(waitingForRole, "Seller", StringComparison.OrdinalIgnoreCase))
        {
            return order.SellerId;
        }
        return null;
    }

    private async Task ApplyResolutionAsync(Order order, string decision, CancellationToken cancellationToken)
    {
        if (string.Equals(decision, "RefundBuyer", StringComparison.OrdinalIgnoreCase))
        {
            var escrow = await _uow.EscrowContracts.Query()
                .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
            if (escrow != null)
            {
                escrow.Status = EscrowStatuses.Refunded;
                escrow.UpdatedAt = DateTime.UtcNow;
                _uow.EscrowContracts.Update(escrow);
            }

            order.Status = OrderStatuses.Cancelled;
            _uow.Orders.Update(order);

            if (order.ListingId.HasValue)
            {
                var listing = await _uow.Listings.GetByIdAsync(order.ListingId.Value);
                if (listing != null && string.Equals(listing.Status, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase))
                {
                    listing.Status = ListingStatuses.Active;
                    listing.UpdatedAt = DateTime.UtcNow;
                    _uow.Listings.Update(listing);
                }
            }
            await _uow.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.Equals(decision, "ReleaseSeller", StringComparison.OrdinalIgnoreCase))
        {
            var escrow = await _uow.EscrowContracts.Query()
                .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
            if (escrow != null)
            {
                escrow.Status = EscrowStatuses.Released;
                escrow.UpdatedAt = DateTime.UtcNow;
                _uow.EscrowContracts.Update(escrow);
            }

            order.Status = OrderStatuses.Completed;
            _uow.Orders.Update(order);

            if (order.ListingId.HasValue)
            {
                var listing = await _uow.Listings.GetByIdAsync(order.ListingId.Value);
                if (listing != null)
                {
                    listing.Status = ListingStatuses.Sold;
                    listing.UpdatedAt = DateTime.UtcNow;
                    _uow.Listings.Update(listing);
                }
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new InvalidOperationException("Invalid resolution decision.");
    }

    private async Task ApplyRejectionAsync(Order order, CancellationToken cancellationToken)
    {
        var paymentPaid = await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == order.OrderId && p.Status == PaymentStatuses.Paid, cancellationToken);

        if (string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = OrderStatuses.Confirmed;
        }
        else
        {
            order.Status = paymentPaid ? OrderStatuses.Confirmed : OrderStatuses.Pending;
        }
        _uow.Orders.Update(order);

        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
        if (escrow != null)
        {
            if (paymentPaid)
            {
                if (string.Equals(escrow.Status, EscrowStatuses.Holding, StringComparison.OrdinalIgnoreCase))
                {
                    escrow.Status = EscrowStatuses.Funded;
                }
            }
            else if (string.Equals(escrow.Status, EscrowStatuses.Holding, StringComparison.OrdinalIgnoreCase))
            {
                escrow.Status = EscrowStatuses.Pending;
            }

            escrow.UpdatedAt = DateTime.UtcNow;
            _uow.EscrowContracts.Update(escrow);
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task LogModActionAsync(ClaimsPrincipal principal, string action, object details, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ActivityLogs.AddAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // swallow logging errors
        }
    }
}
