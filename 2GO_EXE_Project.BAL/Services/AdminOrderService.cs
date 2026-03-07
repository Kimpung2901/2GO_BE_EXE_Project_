using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Validation;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEscrowService _escrowService;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly INotificationService _notificationService;
    private static readonly HashSet<string> AllowedStatuses = new(OrderStatuses.All, StringComparer.OrdinalIgnoreCase);

    public AdminOrderService(IUnitOfWork uow, IEscrowService escrowService, IMarketPriceProvider marketPriceProvider, INotificationService notificationService)
    {
        _uow = uow;
        _escrowService = escrowService;
        _marketPriceProvider = marketPriceProvider;
        _notificationService = notificationService;
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    public async Task<OrderListResponse> GetOrdersAsync(
        string? status,
        long? buyerId,
        long? sellerId,
        long? orderCode,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.ShippingRequests)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!AllowedStatuses.Contains(status))
            {
                throw new InvalidOperationException($"Trạng thái đơn hàng không hợp lệ. Cho phép: {string.Join(", ", OrderStatuses.All)}.");
            }
            query = query.Where(o => o.Status == status);
        }
        if (buyerId.HasValue)
        {
            query = query.Where(o => o.BuyerId == buyerId.Value);
        }
        if (sellerId.HasValue)
        {
            query = query.Where(o => o.SellerId == sellerId.Value);
        }
        if (orderCode.HasValue)
        {
            query = query.Where(o => o.OrderCode == orderCode.Value);
        }
        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= to.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(o => new OrderListItem(
                o.OrderId,
                o.ListingId ?? 0,
                o.BuyerId ?? 0,
                o.SellerId ?? 0,
                o.OrderCode,
                o.PaymentLinkId,
                o.TotalAmount,
                o.PaymentMethod,
                o.Status,
                o.CheckoutUrl,
                o.QrCodeUrl,
                o.PaymentExpiredAt,
                o.CreatedAt,
                o.Listing != null ? o.Listing.Title : null,
                o.Listing != null ? o.Listing.Price : null,
                o.ShippingRequests.Select(s => s.DeliveryAddress).FirstOrDefault(),
                (o.TotalAmount ?? 0m) >= EscrowRules.DepositThresholdAmount,
                ((o.TotalAmount ?? 0m) >= EscrowRules.DepositThresholdAmount) &&
                _uow.Payments.Query().Any(p =>
                    p.OrderId == o.OrderId &&
                    (p.PaymentStage == PaymentStages.Deposit || p.PaymentStage == null) &&
                    p.Status == PaymentStatuses.Paid)))
            .ToListAsync(cancellationToken);

        return new OrderListResponse(total, items);
    }

    public async Task<OrderDetailResponse?> GetByIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.ShippingRequests)
            .Include(o => o.Escrow)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order == null) return null;

        var depositRequired = (order.TotalAmount ?? 0m) >= EscrowRules.DepositThresholdAmount;
        var depositPaid = depositRequired && await IsDepositPaidAsync(order.OrderId, cancellationToken);

        return new OrderDetailResponse(
            order.OrderId,
            order.ListingId ?? 0,
            order.BuyerId ?? 0,
            order.SellerId ?? 0,
            order.EscrowId,
            order.OrderCode,
            order.PaymentLinkId,
            order.TotalAmount,
            order.PaymentMethod,
            order.Status,
            order.CheckoutUrl,
            order.QrCodeUrl,
            order.PaymentExpiredAt,
            order.CreatedAt,
            order.Listing?.Title,
            order.Listing?.Price,
            order.Buyer?.Email,
            order.Buyer?.Phone,
            order.Seller?.Email,
            order.Seller?.Phone,
            order.ShippingRequests.Select(s => s.DeliveryAddress).FirstOrDefault(),
            order.DeliveryPhone,
            order.Escrow?.DepositAmount,
            order.Escrow?.DepositDeadlineAt,
            depositRequired,
            depositPaid,
            order.Escrow?.Status);
    }

    public async Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal adminPrincipal, long orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateUpdateOrderStatus(request));
        var order = await _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.ShippingRequests)
            .Include(o => o.Escrow)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order == null) return new BasicResponse(false, "Không tìm thấy đơn hàng.");

        if (!AllowedStatuses.Contains(request.Status))
        {
            return new BasicResponse(false, "Giá trị trạng thái không hợp lệ.");
        }

        if (string.Equals(order.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Đơn hàng đã ở trạng thái yêu cầu.");
        }

        if (!IsStatusTransitionAllowed(order.Status, request.Status))
        {
            return new BasicResponse(false, $"Chuyển trạng thái đơn hàng không hợp lệ: {order.Status} -> {request.Status}.");
        }

        if (string.Equals(request.Status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return await CancelOrderAsync(adminPrincipal, order, request.Reason, cancellationToken);
        }
        if (string.Equals(request.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            return await ConfirmOrderAsync(adminPrincipal, order, request.Reason, cancellationToken);
        }
        if (string.Equals(request.Status, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return await CompleteOrderAsync(adminPrincipal, order, request.Reason, cancellationToken);
        }
        if (string.Equals(request.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return await DisputeOrderAsync(adminPrincipal, order, request.Reason, cancellationToken);
        }

        order.Status = request.Status;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await LogAdminActionAsync(adminPrincipal, "UpdateOrderStatus", new { order.OrderId, To = request.Status, request.Reason }, cancellationToken);
        return new BasicResponse(true, "Đã cập nhật trạng thái đơn hàng.");
    }

    private static bool IsStatusTransitionAllowed(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current)) return true;

        if (string.Equals(current, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, OrderStatuses.Delivering, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, OrderStatuses.Delivering, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (string.Equals(current, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }

    private async Task<BasicResponse> CancelOrderAsync(ClaimsPrincipal adminPrincipal, Order order, string? reason, CancellationToken cancellationToken)
    {
        var fromStatus = order.Status;
        order.Status = OrderStatuses.Cancelled;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        var escrow = await _uow.EscrowContracts.Query()
            .FirstOrDefaultAsync(e => e.OrderId == order.OrderId, cancellationToken);
        var now = DateTime.UtcNow;
        var canForfeit = escrow != null &&
                         string.Equals(escrow.Status, EscrowStatuses.Funded, StringComparison.OrdinalIgnoreCase) &&
                         escrow.DepositDeadlineAt.HasValue &&
                         now > escrow.DepositDeadlineAt.Value;
        if (canForfeit)
        {
            await _escrowService.ForfeitDepositForOrderAsync(order.OrderId, reason ?? "Admin cancelled after deposit deadline", cancellationToken);
            if (order.SellerId.HasValue && escrow?.DepositAmount != null && escrow.DepositAmount > 0)
            {
                await NotifyAsync(
                    order.SellerId.Value,
                    "ORDER",
                    "Cọc bị tịch thu",
                    $"Đơn hàng #{order.OrderId} bị hủy sau hạn cọc. Cọc {escrow.DepositAmount} sẽ được chuyển cho bạn.",
                    $"/orders/{order.OrderId}",
                    cancellationToken);
            }
        }
        else
        {
            await UpdatePaymentStatusAsync(order.OrderId, PaymentStatuses.Cancelled, PaymentStages.Deposit, cancellationToken);
            await _escrowService.RefundForOrderAsync(order.OrderId, cancellationToken);
            if (order.SellerId.HasValue && escrow?.DepositAmount != null && escrow.DepositAmount > 0)
            {
                await NotifyAsync(
                    order.SellerId.Value,
                    "ORDER",
                    "Cọc được hoàn cho người mua",
                    $"Đơn hàng #{order.OrderId} bị hủy trước hạn. Cọc {escrow.DepositAmount} đã hoàn cho người mua.",
                    $"/orders/{order.OrderId}",
                    cancellationToken);
            }
        }
        await RestoreListingIfReservedAsync(order, cancellationToken);

        var cancelText = OrderNotificationText.ForStatus(OrderStatuses.Cancelled, order.OrderId);
        if (order.BuyerId.HasValue)
        {
            await NotifyAsync(order.BuyerId.Value, "ORDER", cancelText.Title, cancelText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "ORDER", cancelText.Title, cancelText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "UpdateOrderStatus", new { order.OrderId, From = fromStatus, To = order.Status, reason }, cancellationToken);
        return new BasicResponse(true, "Đơn hàng đã bị hủy.");
    }

    private async Task<BasicResponse> ConfirmOrderAsync(ClaimsPrincipal adminPrincipal, Order order, string? reason, CancellationToken cancellationToken)
    {
        if (!string.Equals(order.PaymentMethod, PaymentMethods.COD, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Đơn hàng không COD được xác nhận khi thanh toán được xác minh.");
        }

        var fromStatus = order.Status;
        order.Status = OrderStatuses.Confirmed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.EnsureForOrderAsync(order, null, cancellationToken);

        var confirmText = OrderNotificationText.ForStatus(OrderStatuses.Confirmed, order.OrderId);
        if (order.BuyerId.HasValue)
        {
            await NotifyAsync(order.BuyerId.Value, "ORDER", confirmText.Title, confirmText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "UpdateOrderStatus", new { order.OrderId, From = fromStatus, To = order.Status, reason }, cancellationToken);
        return new BasicResponse(true, "Đơn hàng đã được xác nhận.");
    }

    private async Task<BasicResponse> CompleteOrderAsync(ClaimsPrincipal adminPrincipal, Order order, string? reason, CancellationToken cancellationToken)
    {
        if (!string.Equals(order.Status, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Chỉ đơn hàng đã giao mới có thể hoàn tất.");
        }
        var totalAmount = order.TotalAmount ?? 0m;
        var requiresDeposit = totalAmount >= EscrowRules.DepositThresholdAmount;
        var requiredStage = requiresDeposit ? PaymentStages.Remaining : PaymentStages.Remaining;
        if (!await IsPaymentPaidAsync(order.OrderId, requiredStage, cancellationToken))
        {
            if (string.Equals(order.PaymentMethod, PaymentMethods.COD, StringComparison.OrdinalIgnoreCase))
            {
                await UpdatePaymentStatusAsync(order.OrderId, PaymentStatuses.Paid, requiredStage, cancellationToken);
            }
            else
            {
                return new BasicResponse(false, requiresDeposit
                    ? "Cần thanh toán phần còn lại trước khi hoàn tất đơn hàng."
                    : "Cần thanh toán trước khi hoàn tất đơn hàng.");
            }
        }

        var fromStatus = order.Status;
        order.Status = OrderStatuses.Completed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.ReleaseForOrderAsync(order.OrderId, cancellationToken);
        await _escrowService.PayoutDepositForCompletedOrderAsync(order.OrderId, cancellationToken);
        await _escrowService.PayoutRemainingForCompletedOrderAsync(order.OrderId, cancellationToken);
        await MarkListingSoldAsync(order, cancellationToken);

        var completeText = OrderNotificationText.ForStatus(OrderStatuses.Completed, order.OrderId);
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "ORDER", completeText.Title, completeText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "UpdateOrderStatus", new { order.OrderId, From = fromStatus, To = order.Status, reason }, cancellationToken);
        return new BasicResponse(true, "Đơn hàng đã hoàn tất.");
    }

    private async Task<BasicResponse> DisputeOrderAsync(ClaimsPrincipal adminPrincipal, Order order, string? reason, CancellationToken cancellationToken)
    {
        var fromStatus = order.Status;
        order.Status = OrderStatuses.Disputed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        var disputeText = OrderNotificationText.ForStatus(OrderStatuses.Disputed, order.OrderId);
        if (order.BuyerId.HasValue)
        {
            await NotifyAsync(order.BuyerId.Value, "ORDER", disputeText.Title, disputeText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "ORDER", disputeText.Title, disputeText.Message, $"/orders/{order.OrderId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "UpdateOrderStatus", new { order.OrderId, From = fromStatus, To = order.Status, reason }, cancellationToken);
        return new BasicResponse(true, "Đơn hàng đã được đánh dấu tranh chấp.");
    }

    private async Task<bool> IsPaymentPaidAsync(long orderId, string stage, CancellationToken cancellationToken)
    {
        return await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == orderId &&
                           (p.PaymentStage == stage ||
                            (p.PaymentStage == null && stage == PaymentStages.Deposit)) &&
                           p.Status == PaymentStatuses.Paid, cancellationToken);
    }

    private async Task<bool> IsDepositPaidAsync(long orderId, CancellationToken cancellationToken)
    {
        return await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == orderId &&
                           (p.PaymentStage == PaymentStages.Deposit || p.PaymentStage == null) &&
                           p.Status == PaymentStatuses.Paid, cancellationToken);
    }

    private async Task UpdatePaymentStatusAsync(long orderId, string status, string stage, CancellationToken cancellationToken)
    {
        var payment = await _uow.Payments.Query()
            .FirstOrDefaultAsync(p => p.OrderId == orderId &&
                                      (p.PaymentStage == stage ||
                                       (p.PaymentStage == null && stage == PaymentStages.Deposit)), cancellationToken);
        if (payment == null) return;
        if (!string.Equals(payment.Status, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        payment.Status = status;
        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task RestoreListingIfReservedAsync(Order order, CancellationToken cancellationToken)
    {
        var listingIds = new List<long>();
        if (order.ListingId.HasValue) listingIds.Add(order.ListingId.Value);

        if (listingIds.Count == 0)
        {
            var orderItems = await _uow.OrderItems.Query()
                .Where(oi => oi.OrderId == order.OrderId && oi.ListingId.HasValue)
                .ToListAsync(cancellationToken);
            listingIds.AddRange(orderItems.Select(oi => oi.ListingId!.Value));
        }

        if (listingIds.Count == 0) return;

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
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkListingSoldAsync(Order order, CancellationToken cancellationToken)
    {
        var listingIds = new List<long>();
        if (order.ListingId.HasValue) listingIds.Add(order.ListingId.Value);

        if (listingIds.Count == 0)
        {
            var orderItems = await _uow.OrderItems.Query()
                .Where(oi => oi.OrderId == order.OrderId && oi.ListingId.HasValue)
                .ToListAsync(cancellationToken);
            listingIds.AddRange(orderItems.Select(oi => oi.ListingId!.Value));
        }

        if (listingIds.Count == 0) return;

        var listings = await _uow.Listings.Query()
            .Where(l => listingIds.Contains(l.ListingId))
            .ToListAsync(cancellationToken);

        foreach (var listing in listings)
        {
            listing.Status = ListingStatuses.Sold;
            listing.AvailableQuantity = 0;
            listing.UpdatedAt = DateTime.UtcNow;
            _uow.Listings.Update(listing);
            var soldPrice = order.ListingId.HasValue && order.ListingId.Value == listing.ListingId
                ? order.TotalAmount
                : listing.Price;
            await _marketPriceProvider.TrackListingAsync(listing, soldPrice, "completed_sale", cancellationToken);
        }
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task LogAdminActionAsync(ClaimsPrincipal principal, string action, object details, CancellationToken cancellationToken)
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
            // ignore logging failures
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






