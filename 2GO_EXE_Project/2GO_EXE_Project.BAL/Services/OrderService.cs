using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private const string StatusPending = "Pending";
    private const string StatusCancelled = "Cancelled";
    private const string StatusConfirmed = "Confirmed";
    private const string StatusCompleted = "Completed";

    public OrderService(IUnitOfWork uow)
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

    public async Task<OrderResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var buyerId = GetUserId(userPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == request.ListingId, cancellationToken);
        if (listing == null || !string.Equals(listing.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Listing not available.");
        }
        if (!listing.SellerId.HasValue)
        {
            throw new InvalidOperationException("Seller not found.");
        }

        var order = new Order
        {
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            ListingId = listing.ListingId,
            TotalAmount = listing.Price,
            Status = StatusPending,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var orderItem = new OrderItem
        {
            OrderId = order.OrderId,
            ListingId = listing.ListingId,
            Price = listing.Price
        };
        await _uow.OrderItems.AddAsync(orderItem, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new OrderResponse(order.OrderId, listing.ListingId, buyerId, listing.SellerId.Value, order.TotalAmount, order.Status, order.CreatedAt);
    }

    public async Task<BasicResponse> CancelAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Not allowed.");
        if (!string.Equals(order.Status, StatusPending, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Only pending orders can be cancelled.");
        }

        order.Status = StatusCancelled;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Order cancelled.");
    }

    public async Task<BasicResponse> ConfirmAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.SellerId != userId) return new BasicResponse(false, "Not allowed.");
        if (!string.Equals(order.Status, StatusPending, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Only pending orders can be confirmed.");
        }

        order.Status = StatusConfirmed;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Order confirmed.");
    }

    public async Task<BasicResponse> CompleteAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return new BasicResponse(false, "Order not found.");
        if (order.BuyerId != userId) return new BasicResponse(false, "Not allowed.");
        if (!string.Equals(order.Status, StatusConfirmed, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Only confirmed orders can be completed.");
        }

        order.Status = StatusCompleted;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Order completed.");
    }
}
