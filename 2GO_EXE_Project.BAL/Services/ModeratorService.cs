using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class ModeratorService : IModeratorService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public ModeratorService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
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
            if (!UserStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Trạng thái người dùng không hợp lệ. Cho phép: {string.Join(", ", UserStatuses.All)}.");
            }
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
                u.UserVerifications.Select(v => v.EmailVerified).FirstOrDefault() ?? false,
                u.UserVerifications.Select(v => v.PhoneVerified).FirstOrDefault() ?? false,
                u.UserProfiles.Select(p => p.FullName).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new AdminUserListResponse(total, users);
    }

    public async Task<BasicResponse> BanUserAsync(ClaimsPrincipal modPrincipal, long userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateBanUser(request));
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        user.Status = UserStatuses.Banned;
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
        return new BasicResponse(true, "Đã cấm người dùng.");
    }

    public async Task<BasicResponse> UnbanUserAsync(ClaimsPrincipal modPrincipal, long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        user.Status = UserStatuses.Active;
        user.BanUntil = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "UnbanUser", new { TargetUserId = userId }, cancellationToken);
        return new BasicResponse(true, "Đã bị cấm người dùng.");
    }

    public async Task<ModeratorReportListResponse> GetReportsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Reports.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ReportStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Trạng thái báo cáo không hợp lệ. Cho phép: {string.Join(", ", ReportStatuses.All)}.");
            }
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
        return new ReportDetail(report.ReportId, report.OrderId, report.ReporterId, report.TargetUserId, report.ListingId, report.Reason, ParseEvidenceUrls(report.EvidenceUrls), report.Status, report.WaitingForUserId, report.CreatedAt);
    }

    public async Task<BasicResponse> ResolveReportAsync(ClaimsPrincipal modPrincipal, long reportId, ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateResolveReport(request));
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            return new BasicResponse(false, "Không tìm thấy báo cáo.");
        }

        var currentStatus = string.IsNullOrWhiteSpace(report.Status) ? ReportStatuses.Open : report.Status!;
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return new BasicResponse(false, "Trạng thái là bắt buộc.");
        }

        var nextStatus = request.Status.Trim();
        if (!ReportStatuses.All.Contains(nextStatus, StringComparer.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, $"Trạng thái báo cáo không hợp lệ. Cho phép: {string.Join(", ", ReportStatuses.All)}.");
        }
        if (!IsTransitionAllowed(currentStatus, nextStatus))
        {
            return new BasicResponse(false, $"Chuyển trạng thái báo cáo không hợp lệ: {currentStatus} -> {nextStatus}.");
        }

        if (string.Equals(nextStatus, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.WaitingForRole))
            {
                return new BasicResponse(false, "WaitingForRole là bắt buộc.");
            }
            if (!report.OrderId.HasValue)
            {
                return new BasicResponse(false, "OrderId là bắt buộc.");
            }

            var order = await _uow.Orders.GetByIdAsync(report.OrderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Không tìm thấy đơn hàng.");
            }

            var waitingUserId = ResolveWaitingUserId(order, request.WaitingForRole);
            if (!waitingUserId.HasValue)
            {
                return new BasicResponse(false, "Waiting Không tìm thấy người dùng.");
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
                return new BasicResponse(false, "Cần Decision để giải quyết báo cáo.");
            }
            var orderId = report.OrderId;
            if (!orderId.HasValue)
            {
                return new BasicResponse(false, "OrderId là bắt buộc.");
            }
            var order = await _uow.Orders.GetByIdAsync(orderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Không tìm thấy đơn hàng.");
            }
            await ApplyResolutionAsync(order, request.Decision, cancellationToken);
        }
        else if (string.Equals(nextStatus, ReportStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            var orderId = report.OrderId;
            if (!orderId.HasValue)
            {
                return new BasicResponse(false, "OrderId là bắt buộc.");
            }
            var order = await _uow.Orders.GetByIdAsync(orderId.Value);
            if (order == null)
            {
                return new BasicResponse(false, "Không tìm thấy đơn hàng.");
            }
            await ApplyRejectionAsync(order, cancellationToken);
        }

        report.Status = nextStatus;
        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(cancellationToken);

        await NotifyReportStatusAsync(report, nextStatus, cancellationToken);
        await LogModActionAsync(modPrincipal, "ResolveReport", new { ReportId = reportId, report.Status, request.WaitingForRole, request.Decision, request.Note }, cancellationToken);
        return new BasicResponse(true, "Đã cập nhật báo cáo.");
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
                    listing.AvailableQuantity = 1;
                    listing.UpdatedAt = DateTime.UtcNow;
                    _uow.Listings.Update(listing);
                }
            }
            else
            {
                await RestoreListingsForOrderAsync(order, cancellationToken);
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
                    listing.AvailableQuantity = 0;
                    listing.UpdatedAt = DateTime.UtcNow;
                    _uow.Listings.Update(listing);
                }
            }
            else
            {
                await MarkListingsSoldAsync(order, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return;
        }

        throw new InvalidOperationException("Quyết định giải quyết không hợp lệ.");
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

    private async Task RestoreListingsForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        var listingIds = new List<long>();
        if (order.ListingId.HasValue) listingIds.Add(order.ListingId.Value);

        if (listingIds.Count == 0)
        {
            var items = await _uow.OrderItems.Query()
                .Where(oi => oi.OrderId == order.OrderId && oi.ListingId.HasValue)
                .ToListAsync(cancellationToken);
            listingIds.AddRange(items.Select(oi => oi.ListingId!.Value));
        }

        var listings = await _uow.Listings.Query()
            .Where(l => listingIds.Contains(l.ListingId))
            .ToListAsync(cancellationToken);

        foreach (var listing in listings)
        {
            if (string.Equals(listing.Status, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase))
            {
                listing.Status = ListingStatuses.Active;
                listing.AvailableQuantity = 1;
                listing.UpdatedAt = DateTime.UtcNow;
                _uow.Listings.Update(listing);
            }
        }
    }

    private async Task MarkListingsSoldAsync(Order order, CancellationToken cancellationToken)
    {
        var listingIds = new List<long>();
        if (order.ListingId.HasValue) listingIds.Add(order.ListingId.Value);

        if (listingIds.Count == 0)
        {
            var items = await _uow.OrderItems.Query()
                .Where(oi => oi.OrderId == order.OrderId && oi.ListingId.HasValue)
                .ToListAsync(cancellationToken);
            listingIds.AddRange(items.Select(oi => oi.ListingId!.Value));
        }

        var listings = await _uow.Listings.Query()
            .Where(l => listingIds.Contains(l.ListingId))
            .ToListAsync(cancellationToken);

        foreach (var listing in listings)
        {
            listing.Status = ListingStatuses.Sold;
            listing.AvailableQuantity = 0;
            listing.UpdatedAt = DateTime.UtcNow;
            _uow.Listings.Update(listing);
        }
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

    private static IReadOnlyList<string>? ParseEvidenceUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task NotifyReportStatusAsync(Report report, string nextStatus, CancellationToken cancellationToken)
    {
        var text = ReportNotificationText.ForStatus(nextStatus, report.ReportId);
        if (string.Equals(nextStatus, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase) && report.WaitingForUserId.HasValue)
        {
            await NotifyAsync(report.WaitingForUserId.Value, "REPORT", text.Title, text.Message, $"/reports/{report.ReportId}", cancellationToken);
            return;
        }

        if (string.Equals(nextStatus, ReportStatuses.Resolved, StringComparison.OrdinalIgnoreCase))
        {
            if (report.ReporterId.HasValue)
            {
                await NotifyAsync(report.ReporterId.Value, "REPORT", text.Title, text.Message, $"/reports/{report.ReportId}", cancellationToken);
            }
            if (report.TargetUserId.HasValue)
            {
                await NotifyAsync(report.TargetUserId.Value, "REPORT", text.Title, text.Message, $"/reports/{report.ReportId}", cancellationToken);
            }
            return;
        }

        if (string.Equals(nextStatus, ReportStatuses.Rejected, StringComparison.OrdinalIgnoreCase) && report.ReporterId.HasValue)
        {
            await NotifyAsync(report.ReporterId.Value, "REPORT", text.Title, text.Message, $"/reports/{report.ReportId}", cancellationToken);
        }
    }
private async Task NotifyAsync(long userId, string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest(
                userId,
                title,
                message,
                type,
                link), cancellationToken);
        }
        catch
        {
            // ignore notification failures
        }
    }
}






