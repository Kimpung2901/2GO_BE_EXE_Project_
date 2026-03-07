using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Reports;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;
using ReportListResponse = _2GO_EXE_Project.BAL.DTOs.Reports.ReportListResponse;

namespace _2GO_EXE_Project.BAL.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly string _cloudinaryCloudName;

    public ReportService(IUnitOfWork uow, INotificationService notificationService, IOptions<CloudinarySettings> cloudinaryOptions)
    {
        _uow = uow;
        _notificationService = notificationService;
        _cloudinaryCloudName = cloudinaryOptions.Value.CloudName ?? string.Empty;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    public async Task<ReportResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCreateReport(request));
        var userId = GetUserId(userPrincipal);
        var evidenceUrls = NormalizeEvidenceUrls(request.EvidenceUrls);
        ValidateCloudinaryEvidenceUrls(evidenceUrls);

        var order = await _uow.Orders.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            throw new InvalidOperationException("Không tìm thấy don hàng.");
        }

        var isBuyer = order.BuyerId == userId;
        var isSeller = order.SellerId == userId;
        if (!isBuyer && !isSeller)
        {
            throw new InvalidOperationException("Chỉ người mua hoặc người bán mới có thể báo cáo đơn hàng này.");
        }

        var targetUserId = isBuyer ? order.SellerId : order.BuyerId;
        if (!targetUserId.HasValue)
        {
            throw new InvalidOperationException("Không tìm thấy người dùng mục tiêu.");
        }
        if (request.TargetUserId.HasValue && request.TargetUserId.Value != targetUserId.Value)
        {
            throw new InvalidOperationException("Người dùng mục tiêu không khớp với ngữ cảnh đơn hàng.");
        }

        var hasActive = await _uow.Reports.Query()
            .AnyAsync(r => r.OrderId == order.OrderId && IsActiveStatus(r.Status), cancellationToken);
        if (hasActive)
        {
            throw new InvalidOperationException("Đơn hàng đã có báo cáo đang xử lý.");
        }

        var report = new Report
        {
            ReporterId = userId,
            OrderId = order.OrderId,
            ListingId = order.ListingId,
            TargetUserId = targetUserId,
            Reason = request.Reason,
            EvidenceUrls = evidenceUrls.Count > 0 ? JsonSerializer.Serialize(evidenceUrls) : null,
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

        if (targetUserId.HasValue)
        {
            await NotifyAsync(targetUserId.Value, "REPORT", "Bạn bị báo cáo", $"Một báo cáo mới đã được tạo (ID #{report.ReportId}).", $"/reports/{report.ReportId}", cancellationToken);
        }
        await NotifyAdminsAsync("REPORT", "Có báo cáo mới", $"Có báo cáo mới (ID #{report.ReportId}).", $"/admin/reports/{report.ReportId}", cancellationToken);

        return new ReportResponse(report.ReportId, report.OrderId ?? 0, report.ReporterId, report.TargetUserId, report.Reason, ParseEvidenceUrls(report.EvidenceUrls), report.Status, report.WaitingForUserId, report.CreatedAt);
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
            .Select(r => new ReportResponse(r.ReportId, r.OrderId ?? 0, r.ReporterId, r.TargetUserId, r.Reason, ParseEvidenceUrls(r.EvidenceUrls), r.Status, r.WaitingForUserId, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReportListResponse(total, items);
    }

    public async Task<BasicResponse> ReplyAsync(ClaimsPrincipal userPrincipal, long reportId, ReplyReportRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateReplyReport(request));
        var userId = GetUserId(userPrincipal);

        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            return new BasicResponse(false, "Không tìm thấy báo cáo.");
        }
        if (!string.Equals(report.Status, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Báo cáo không ở trạng thái chờ phản hồi.");
        }
        if (!report.WaitingForUserId.HasValue || report.WaitingForUserId.Value != userId)
        {
            return new BasicResponse(false, "Không có quyền phản hồi báo cáo này.");
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

        var otherUserId = report.ReporterId == userId ? report.TargetUserId : report.ReporterId;
        if (otherUserId.HasValue)
        {
            await NotifyAsync(otherUserId.Value, "REPORT", "Có phản hồi báo cáo", $"Báo cáo #{report.ReportId} đã có phản hồi mới.", $"/reports/{report.ReportId}", cancellationToken);
        }

        return new BasicResponse(true, "Đã gửi phản hồi.");
    }

    private static bool IsActiveStatus(string? status)
    {
        return string.Equals(status, ReportStatuses.Open, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, ReportStatuses.InReview, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, ReportStatuses.WaitingOtherParty, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> NormalizeEvidenceUrls(IReadOnlyList<string>? evidenceUrls)
    {
        if (evidenceUrls == null || evidenceUrls.Count == 0) return new List<string>();
        return evidenceUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToList();
    }

    private void ValidateCloudinaryEvidenceUrls(IReadOnlyList<string> evidenceUrls)
    {
        if (evidenceUrls.Count == 0) return;
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(_cloudinaryCloudName))
        {
            result.Add("evidenceUrls", "Cloudinary CloudName chưa được cấu hình.");
            ValidationGuard.ThrowIfInvalid(result);
        }

        var cloudPathPrefix = $"/{_cloudinaryCloudName.Trim()}/";
        for (var i = 0; i < evidenceUrls.Count; i++)
        {
            var url = evidenceUrls[i];
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                result.Add($"evidenceUrls[{i}]", "Evidence url phải là URL Cloudinary tuyệt đối hợp lệ.");
                continue;
            }

            if (!string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"evidenceUrls[{i}]", "Evidence url phải được lưu trữ trên Cloudinary.");
                continue;
            }

            if (!uri.AbsolutePath.StartsWith(cloudPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"evidenceUrls[{i}]", "Evidence url phải thuộc Cloudinary cloud đã cấu hình.");
            }
        }

        ValidationGuard.ThrowIfInvalid(result);
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

    private async Task NotifyAdminsAsync(string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            var adminIds = await _uow.Users.Query()
                .Where(u => u.Role == UserRoles.Admin || u.Role == UserRoles.Manager)
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            foreach (var id in adminIds)
            {
                await _notificationService.CreateAsync(new CreateNotificationRequest(
                    id,
                    title,
                    message,
                    type,
                    link), cancellationToken);
            }
        }
        catch
        {
            // ignore notification failures
        }
    }
}











