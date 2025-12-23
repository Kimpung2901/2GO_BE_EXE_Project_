using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Shipping;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ShippingService : IShippingService
{
    private readonly IUnitOfWork _uow;

    public ShippingService(IUnitOfWork uow)
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

    public async Task<ShippingResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateShippingRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.SellerId != userId) throw new InvalidOperationException("Only seller can create shipping.");

        var existing = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
        if (existing != null)
        {
            return new ShippingResponse(existing.ShipId, existing.OrderId ?? 0, existing.Provider, existing.TrackingCode, existing.PickupAddress, existing.DeliveryAddress, existing.Status, existing.CreatedAt);
        }

        var ship = new ShippingRequest
        {
            OrderId = request.OrderId,
            Provider = request.Provider,
            PickupAddress = request.PickupAddress,
            DeliveryAddress = request.DeliveryAddress,
            Status = ShippingStatuses.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ShippingRequests.AddAsync(ship, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ShippingResponse(ship.ShipId, ship.OrderId ?? 0, ship.Provider, ship.TrackingCode, ship.PickupAddress, ship.DeliveryAddress, ship.Status, ship.CreatedAt);
    }

    public async Task<ShippingResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return null;
        if (order.BuyerId != userId && order.SellerId != userId) return null;

        var ship = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
        if (ship == null) return null;

        return new ShippingResponse(ship.ShipId, ship.OrderId ?? 0, ship.Provider, ship.TrackingCode, ship.PickupAddress, ship.DeliveryAddress, ship.Status, ship.CreatedAt);
    }

    public async Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal userPrincipal, long shipId, UpdateShippingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var ship = await _uow.ShippingRequests.GetByIdAsync(shipId);
        if (ship == null) return new BasicResponse(false, "Shipping not found.");

        var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
        if (order == null || order.SellerId != userId) return new BasicResponse(false, "Not allowed.");

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return new BasicResponse(false, "Status is required.");
        }

        if (string.Equals(ship.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Shipping already in requested status.");
        }

        var current = ship.Status ?? ShippingStatuses.Requested;
        if (!IsShippingTransitionAllowed(current, request.Status))
        {
            return new BasicResponse(false, $"Invalid shipping status transition: {current} -> {request.Status}.");
        }

        ship.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.TrackingCode))
        {
            ship.TrackingCode = request.TrackingCode;
        }
        _uow.ShippingRequests.Update(ship);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Shipping updated.");
    }

    private static bool IsShippingTransitionAllowed(string current, string next)
    {
        if (string.Equals(current, ShippingStatuses.Requested, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ShippingStatuses.InTransit, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ShippingStatuses.Failed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ShippingStatuses.InTransit, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ShippingStatuses.Failed, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
