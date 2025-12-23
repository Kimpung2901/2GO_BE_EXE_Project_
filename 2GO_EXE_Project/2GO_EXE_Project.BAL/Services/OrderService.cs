using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEscrowService _escrowService;

    public OrderService(IUnitOfWork uow, IEscrowService escrowService)
    {
        _uow = uow;
        _escrowService = escrowService;
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

    public async Task<OrderResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var buyerId = GetUserId(userPrincipal);
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            throw new InvalidOperationException("Payment method is required.");
        }
        var method = NormalizePaymentMethod(request.PaymentMethod);

        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == request.ListingId, cancellationToken);
        if (listing == null || !string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Listing not available.");
        }
        if (!listing.SellerId.HasValue)
        {
            throw new InvalidOperationException("Seller not found.");
        }
        if (listing.SellerId.Value == buyerId)
        {
            throw new InvalidOperationException("You cannot order your own listing.");
        }

        var hasActiveOrder = await _uow.Orders.Query()
            .AnyAsync(o => o.ListingId == listing.ListingId &&
                           (o.Status == OrderStatuses.Pending || o.Status == OrderStatuses.Confirmed || o.Status == OrderStatuses.Completed || o.Status == OrderStatuses.Disputed),
                cancellationToken);
        if (hasActiveOrder)
        {
            throw new InvalidOperationException("Listing already has an active order.");
        }

        var order = new Order
        {
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            ListingId = listing.ListingId,
            TotalAmount = listing.Price,
            PaymentMethod = method,
            Status = OrderStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var payment = new Payment
        {
            UserId = buyerId,
            OrderId = order.OrderId,
            Amount = order.TotalAmount,
            Method = method,
            Status = PaymentStatuses.Pending,
            ReferenceCode = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Payments.AddAsync(payment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _escrowService.EnsureForOrderAsync(order, payment.PaymentId, cancellationToken);

        listing.Status = ListingStatuses.Reserved;
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

        await LogOrderActionAsync(buyerId, "OrderCreated", new { order.OrderId, order.ListingId, order.Status }, cancellationToken);

        return new OrderResponse(order.OrderId, listing.ListingId, buyerId, listing.SellerId.Value, order.TotalAmount, order.PaymentMethod, payment.Status, order.Status, order.CreatedAt);
    }

    public async Task<OrderListResponse> GetMyOrdersAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var query = _uow.Orders.Query()
            .Include(o => o.Listing)
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
                o.TotalAmount,
                o.PaymentMethod,
                _uow.Payments.Query().Where(p => p.OrderId == o.OrderId).Select(p => p.Status).FirstOrDefault(),
                o.Status,
                o.CreatedAt,
                o.Listing != null ? o.Listing.Title : null,
                o.Listing != null ? o.Listing.Price : null))
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
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order == null) return null;
        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return null;
        }

        return new OrderDetailResponse(
            order.OrderId,
            order.ListingId ?? 0,
            order.BuyerId ?? 0,
            order.SellerId ?? 0,
            order.TotalAmount,
            order.PaymentMethod,
            await _uow.Payments.Query().Where(p => p.OrderId == order.OrderId).Select(p => p.Status).FirstOrDefaultAsync(cancellationToken),
            order.Status,
            order.CreatedAt,
            order.Listing?.Title,
            order.Listing?.Price,
            order.Buyer?.Email,
            order.Buyer?.Phone,
            order.Seller?.Email,
            order.Seller?.Phone);
    }

    public async Task<BasicResponse> CancelAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Not allowed.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Order is in dispute.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Order already cancelled.");
            }
            return new BasicResponse(false, "Only pending orders can be cancelled.");
        }

        order.Status = OrderStatuses.Cancelled;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await UpdatePaymentStatusAsync(order.OrderId, PaymentStatuses.Cancelled, cancellationToken);
        await _escrowService.RefundForOrderAsync(order.OrderId, cancellationToken);
        await RestoreListingIfReservedAsync(order.ListingId, cancellationToken);
        await LogOrderActionAsync(userId, "OrderCancelled", new { order.OrderId, order.Status }, cancellationToken);
        return new BasicResponse(true, "Order cancelled.");
    }

    public async Task<BasicResponse> ConfirmAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.SellerId != userId) return new BasicResponse(false, "Not allowed.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Order is in dispute.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Order already confirmed.");
            }
            return new BasicResponse(false, "Only pending orders can be confirmed.");
        }

        if (!string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Non-COD orders are confirmed by payment verification.");
        }

        order.Status = OrderStatuses.Confirmed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.EnsureForOrderAsync(order, null, cancellationToken);
        await LogOrderActionAsync(userId, "OrderConfirmed", new { order.OrderId, order.Status }, cancellationToken);
        return new BasicResponse(true, "Order confirmed.");
    }

    public async Task<BasicResponse> CompleteAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Not allowed.");
        if (string.Equals(order.Status, OrderStatuses.Disputed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Order is in dispute.");
        }
        if (!string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(true, "Order already completed.");
            }
            return new BasicResponse(false, "Only confirmed orders can be completed.");
        }

        if (!await IsPaymentPaidAsync(order.OrderId, cancellationToken))
        {
            if (string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            {
                await UpdatePaymentStatusAsync(order.OrderId, PaymentStatuses.Paid, cancellationToken);
                await _escrowService.FundForOrderAsync(order.OrderId, null, cancellationToken);
            }
            else
            {
                return new BasicResponse(false, "Payment must be paid before completing the order.");
            }
        }

        order.Status = OrderStatuses.Completed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        await _escrowService.ReleaseForOrderAsync(order.OrderId, cancellationToken);
        await MarkListingSoldAsync(order.ListingId, cancellationToken);
        await LogOrderActionAsync(userId, "OrderCompleted", new { order.OrderId, order.Status }, cancellationToken);
        return new BasicResponse(true, "Order completed.");
    }

    private static string NormalizePaymentMethod(string method)
    {
        if (string.Equals(method, "COD", StringComparison.OrdinalIgnoreCase)) return "COD";
        if (string.Equals(method, "VNPAY", StringComparison.OrdinalIgnoreCase)) return "VNPAY";
        if (string.Equals(method, "MOMO", StringComparison.OrdinalIgnoreCase)) return "MOMO";
        throw new InvalidOperationException("Payment method not supported.");
    }

    private async Task<bool> IsPaymentPaidAsync(long orderId, CancellationToken cancellationToken)
    {
        return await _uow.Payments.Query()
            .AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatuses.Paid, cancellationToken);
    }

    private async Task UpdatePaymentStatusAsync(long orderId, string status, CancellationToken cancellationToken)
    {
        var payment = await _uow.Payments.Query()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        if (payment == null) return;
        payment.Status = status;
        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task RestoreListingIfReservedAsync(long? listingId, CancellationToken cancellationToken)
    {
        if (!listingId.HasValue) return;
        var listing = await _uow.Listings.GetByIdAsync(listingId.Value);
        if (listing == null) return;
        if (string.Equals(listing.Status, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase))
        {
            listing.Status = ListingStatuses.Active;
            listing.UpdatedAt = DateTime.UtcNow;
            _uow.Listings.Update(listing);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MarkListingSoldAsync(long? listingId, CancellationToken cancellationToken)
    {
        if (!listingId.HasValue) return;
        var listing = await _uow.Listings.GetByIdAsync(listingId.Value);
        if (listing == null) return;
        listing.Status = ListingStatuses.Sold;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
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
}
