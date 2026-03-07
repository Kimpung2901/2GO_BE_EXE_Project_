using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using System.Threading.Tasks;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEscrowService _escrowService;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly INotificationService _notificationService;
    private const decimal CommissionRateValue = 0.07m;

    public OrderService(IUnitOfWork uow, IEscrowService escrowService, IMarketPriceProvider marketPriceProvider, INotificationService notificationService)
    {
        _uow = uow;
        _escrowService = escrowService;
        _marketPriceProvider = marketPriceProvider;
        _notificationService = notificationService;
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

    public async Task<OrderResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCreateOrder(request));
        var buyerId = GetUserId(userPrincipal);
        var method = NormalizePaymentMethod(request.PaymentMethod);

        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == request.ListingId, cancellationToken);
        if (listing == null || !string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bài đăng không khả dụng.");
        }
        if (!listing.SellerId.HasValue)
        {
            throw new InvalidOperationException("Không tìm thấy người bán.");
        }
        if (listing.SellerId.Value == buyerId)
        {
            throw new InvalidOperationException("Bạn không thể đặt hàng bài đăng của chính mình.");
        }

        var hasActiveOrder = await _uow.Orders.Query()
            .AnyAsync(o => o.ListingId == listing.ListingId &&
                           (o.Status == OrderStatuses.Pending ||
                            o.Status == OrderStatuses.Confirmed ||
                            o.Status == OrderStatuses.Delivering ||
                            o.Status == OrderStatuses.Delivered ||
                            o.Status == OrderStatuses.Completed ||
                            o.Status == OrderStatuses.Disputed),
                cancellationToken);
        if (hasActiveOrder)
        {
            throw new InvalidOperationException("Bài đăng đã có đơn hàng đang hoạt động.");
        }

        var order = new Order
        {
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            ListingId = listing.ListingId,
            TotalAmount = listing.Price,
            PaymentMethod = method,
            Status = OrderStatuses.Pending,
            DeliveryPhone = string.IsNullOrWhiteSpace(request.DeliveryPhone) ? null : request.DeliveryPhone.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (string.Equals(method, PaymentMethods.COD, StringComparison.OrdinalIgnoreCase))
        {
            order.OrderCode = order.OrderId;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        var totalAmount = order.TotalAmount ?? 0m;
        var requiresDeposit = totalAmount >= EscrowRules.DepositThresholdAmount;
        var depositAmount = Math.Round(totalAmount * EscrowRules.DepositRate, 2, MidpointRounding.AwayFromZero);
        var payment = new Payment
        {
            UserId = buyerId,
            OrderId = order.OrderId,
            Amount = requiresDeposit ? depositAmount : totalAmount,
            Method = method,
            Status = PaymentStatuses.Pending,
            PaymentStage = requiresDeposit ? PaymentStages.Deposit : PaymentStages.Remaining,
            PaymentType = PaymentTypes.Commission,
            CommissionRate = CommissionRateValue,
            CommissionBaseAmount = order.TotalAmount,
            ReferenceCode = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Payments.AddAsync(payment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var escrow = await _escrowService.EnsureForOrderAsync(order, payment.PaymentId, cancellationToken);

        listing.Status = ListingStatuses.Reserved;
        listing.AvailableQuantity = 0;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        var orderItem = new OrderItem
        {
            OrderId = order.OrderId,
            ListingId = listing.ListingId,
            Price = listing.Price
        };
        await _uow.OrderItems.AddAsync(orderItem, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var shipping = new ShippingRequest
        {
            OrderId = order.OrderId,
            DeliveryAddress = request.DeliveryAddress,
            Status = null,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.ShippingRequests.AddAsync(shipping, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogOrderActionAsync(buyerId, "OrderCreated", new { order.OrderId, order.ListingId, order.Status }, cancellationToken);
        await NotifyAsync(listing.SellerId.Value, "ORDER", "Có đơn hàng mới", $"Đơn hàng #{order.OrderId} vừa được tạo.", $"/orders/{order.OrderId}", cancellationToken);
        await NotifyAsync(buyerId, "ORDER", "Đã tạo đơn hàng", $"Đơn hàng #{order.OrderId} của bạn đã được tạo.", $"/orders/{order.OrderId}", cancellationToken);

        return new OrderResponse(
            order.OrderId, 
            listing.ListingId, 
            buyerId, 
            listing.SellerId.Value, 
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
            request.DeliveryAddress,
            order.DeliveryPhone,
            escrow.DepositAmount,
            escrow.DepositDeadlineAt,
            requiresDeposit,
            false,
            escrow.Status);
    }

    public async Task<OrderListResponse> GetMyOrdersAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var query = _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.ShippingRequests)
            .Where(o => o.BuyerId == userId || o.SellerId == userId);

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

    public async Task<OrderDetailResponse?> GetByIdAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.ShippingRequests)
            .Include(o => o.Escrow)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order == null) return null;
        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return null;
        }

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

    public async Task<BasicResponse> CancelAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Không tìm thấy đơn hàng.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Không có quyền thực hiện.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Đơn hàng đang tranh chấp.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Đơn hàng đã bị hủy.");
            }
            return new BasicResponse(false, "Chỉ đơn hàng đang chờ hoặc đã xác nhận mới có thể hủy.");
        }

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
            await _escrowService.ForfeitDepositForOrderAsync(order.OrderId, "Buyer cancelled after deposit deadline", cancellationToken);
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
        await LogOrderActionAsync(userId, "OrderCancelled", new { order.OrderId, order.Status }, cancellationToken);
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "ORDER", "Đơn hàng đã hủy", $"Đơn hàng #{order.OrderId} đã bị hủy.", $"/orders/{order.OrderId}", cancellationToken);
        }
        return new BasicResponse(true, "Đơn hàng đã bị hủy.");
    }

    public async Task<BasicResponse> ConfirmAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Không tìm thấy đơn hàng.");
        if (order.SellerId != userId) return new BasicResponse(false, "Không có quyền thực hiện.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Đơn hàng đang tranh chấp.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Đơn hàng đã được xác nhận.");
            }
            return new BasicResponse(false, "Chỉ đơn hàng đang chờ mới có thể xác nhận.");
        }

        if (!string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Đơn hàng không COD được xác nhận khi thanh toán được xác minh.");
        }

        order.Status = OrderStatuses.Confirmed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.EnsureForOrderAsync(order, null, cancellationToken);
        await LogOrderActionAsync(userId, "OrderConfirmed", new { order.OrderId, order.Status }, cancellationToken);
        if (order.BuyerId.HasValue)
        {
            await NotifyAsync(order.BuyerId.Value, "ORDER", "Đơn hàng đã xác nhận", $"Đơn hàng #{order.OrderId} đã được xác nhận.", $"/orders/{order.OrderId}", cancellationToken);
        }
        return new BasicResponse(true, "Đơn hàng đã được xác nhận.");
    }

    public async Task<BasicResponse> CompleteAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Không tìm thấy đơn hàng.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Không có quyền thực hiện.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Đơn hàng đang tranh chấp.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Đơn hàng đã hoàn tất.");
            }
            return new BasicResponse(false, "Chỉ đơn hàng đã giao mới có thể hoàn tất.");
        }

        var totalAmount = order.TotalAmount ?? 0m;
        var requiresDeposit = totalAmount >= EscrowRules.DepositThresholdAmount;
        var requiredStage = requiresDeposit ? PaymentStages.Remaining : PaymentStages.Remaining;
        if (!await IsPaymentPaidAsync(order.OrderId, requiredStage, cancellationToken))
        {
            if (string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
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

        order.Status = OrderStatuses.Completed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.ReleaseForOrderAsync(order.OrderId, cancellationToken);
        await _escrowService.PayoutDepositForCompletedOrderAsync(order.OrderId, cancellationToken);
        await _escrowService.PayoutRemainingForCompletedOrderAsync(order.OrderId, cancellationToken);
        await MarkListingSoldAsync(order, cancellationToken);
        await LogOrderActionAsync(userId, "OrderCompleted", new { order.OrderId, order.Status }, cancellationToken);
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "ORDER", "Đơn hàng đã hoàn tất", $"Đơn hàng #{order.OrderId} đã hoàn tất.", $"/orders/{order.OrderId}", cancellationToken);
        }
        return new BasicResponse(true, "Đơn hàng đã hoàn tất.");
    }

    private static string NormalizePaymentMethod(string method)
    {
        if (string.Equals(method, "COD", StringComparison.OrdinalIgnoreCase)) return "COD";
        if (string.Equals(method, "PAYOS", StringComparison.OrdinalIgnoreCase)) return "PAYOS";
        throw new InvalidOperationException("Phương thức thanh toán không được hỗ trợ.");
    }

    private async Task<bool> IsDepositPaidAsync(long orderId, CancellationToken cancellationToken)
    {
        return await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == orderId &&
                           (p.PaymentStage == PaymentStages.Deposit || p.PaymentStage == null) &&
                           p.Status == PaymentStatuses.Paid, cancellationToken);
    }

    private async Task<bool> IsPaymentPaidAsync(long orderId, string stage, CancellationToken cancellationToken)
    {
        return await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == orderId &&
                           (p.PaymentStage == stage ||
                            (p.PaymentStage == null && stage == PaymentStages.Deposit)) &&
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

    private async Task LogOrderActionAsync(long userId, string action, object details, CancellationToken cancellationToken)
    {
        try
        {
            await _uow.ActivityLogs.AddAsync(new _2GO_EXE_Project.DAL.Entities.ActivityLog
            {
                UserId = userId,
                Action = action,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
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

    public async Task<OrderDetailResponse?> GetOrderByOrderCode(long orderCode, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.ShippingRequests)
            .Include(o => o.Escrow)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
        {
            return null;
        }
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

    public async Task<Order?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        return await _uow.Orders.Query()
            .Include(o => o.Listing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);
    }
}













