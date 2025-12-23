using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Reports;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using ReportListResponse = _2GO_EXE_Project.BAL.DTOs.Reports.ReportListResponse;

namespace _2GO_EXE_Project.BAL.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
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

    public async Task<ReportResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }
        if (request.OrderId <= 0)
        {
            throw new InvalidOperationException("OrderId is required.");
        }

        var order = await _uow.Orders.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        var isBuyer = order.BuyerId == userId;
        var isSeller = order.SellerId == userId;
        if (!isBuyer && !isSeller)
        {
            throw new InvalidOperationException("Only buyer or seller can report this order.");
        }

        var targetUserId = isBuyer ? order.SellerId : order.BuyerId;
        if (!targetUserId.HasValue)
        {
            throw new InvalidOperationException("Target user not found.");
        }
        if (request.TargetUserId.HasValue && request.TargetUserId.Value != targetUserId.Value)
        {
            throw new InvalidOperationException("Target user does not match order context.");
        }

        var hasActive = await _uow.Reports.Query()
            .AnyAsync(r => r.OrderId == order.OrderId && IsActiveStatus(r.Status), cancellationToken);
        if (hasActive)
        {
            throw new InvalidOperationException("Order already has an active report.");
        }

        var report = new Report
        {
            ReporterId = userId,
            OrderId = order.OrderId,
            ListingId = order.ListingId,
            TargetUserId = targetUserId,
            Reason = request.Reason,
            Status = ReportStatuses.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Reports.AddAsync(report, cancellationToken);

        order.Status = OrderStatuses.Disputed;
        _uow.Orders.Update(order);

        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
        if (escrow != null &&
            !string.Equals(escrow.Status, EscrowStatuses.Released, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(escrow.Status, EscrowStatuses.Refunded, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(escrow.Status, EscrowStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            escrow.Status = EscrowStatuses.Holding;
            escrow.UpdatedAt = DateTime.UtcNow;
            _uow.EscrowContracts.Update(escrow);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new ReportResponse(report.ReportId, report.OrderId ?? 0, report.ReporterId, report.TargetUserId, report.Reason, report.Status, report.WaitingForUserId, report.CreatedAt);
    }

    public async Task<ReportListResponse> GetMyReportsAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var query = _uow.Reports.Query()
            .Where(r => r.ReporterId == userId || r.TargetUserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(r => new ReportResponse(r.ReportId, r.OrderId ?? 0, r.ReporterId, r.TargetUserId, r.Reason, r.Status, r.WaitingForUserId, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReportListResponse(total, items);
    }

    public async Task<BasicResponse> ReplyAsync(ClaimsPrincipal userPrincipal, long reportId, ReplyReportRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new BasicResponse(false, "Message is required.");
        }

        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            return new BasicResponse(false, "Report not found.");
        }
        if (!string.Equals(report.Status, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Report is not waiting for a response.");
        }
        if (!report.WaitingForUserId.HasValue || report.WaitingForUserId.Value != userId)
        {
            return new BasicResponse(false, "Not allowed to reply for this report.");
        }

        report.Status = ReportStatuses.InReview;
        report.WaitingForUserId = null;
        _uow.Reports.Update(report);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            UserId = userId,
            Action = "ReportReply",
            Details = JsonSerializer.Serialize(new { ReportId = reportId, request.Message }),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Reply submitted.");
    }

    private static bool IsActiveStatus(string? status)
    {
        return string.Equals(status, ReportStatuses.Open, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, ReportStatuses.InReview, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase);
    }
}
