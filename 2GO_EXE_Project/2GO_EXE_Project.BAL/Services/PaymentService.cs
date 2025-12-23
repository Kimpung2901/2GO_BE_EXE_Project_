using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGateway _gateway;
    private readonly IEscrowService _escrowService;

    public PaymentService(IUnitOfWork uow, IPaymentGateway gateway, IEscrowService escrowService)
    {
        _uow = uow;
        _gateway = gateway;
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

    public async Task<PaymentResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }
        if (order.BuyerId != userId)
        {
            throw new InvalidOperationException("Not allowed.");
        }
        if (!string.Equals(order.PaymentMethod, request.Method, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Payment method does not match the order.");
        }

        var existing = await _uow.Payments.Query()
            .FirstOrDefaultAsync(p => p.OrderId == order.OrderId, cancellationToken);
        if (existing != null)
        {
            return new PaymentResponse(existing.PaymentId, existing.Amount, existing.Method, existing.Status, existing.ReferenceCode, existing.CreatedAt);
        }

        var payment = new Payment
        {
            UserId = userId,
            OrderId = order.OrderId,
            Amount = order.TotalAmount,
            Method = order.PaymentMethod,
            Status = PaymentStatuses.Pending,
            ReferenceCode = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Payments.AddAsync(payment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogPaymentActionAsync(userId, "PaymentCreated", new { payment.PaymentId, payment.Amount, payment.Status }, cancellationToken);

        return new PaymentResponse(payment.PaymentId, payment.Amount, payment.Method, payment.Status, payment.ReferenceCode, payment.CreatedAt);
    }

    public async Task<BasicResponse> VerifyAsync(ClaimsPrincipal userPrincipal, long paymentId, VerifyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var payment = await _uow.Payments.Query()
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.UserId == userId, cancellationToken);
        if (payment == null) return new BasicResponse(false, "Payment not found.");

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return new BasicResponse(false, "Status is required.");
        }

        if (!PaymentStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Invalid payment status.");
        }

        if (string.Equals(payment.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Payment already in requested status.");
        }

        if (!IsPaymentTransitionAllowed(payment.Status, request.Status))
        {
            return new BasicResponse(false, $"Invalid payment status transition: {payment.Status} -> {request.Status}.");
        }

        if (string.Equals(payment.Method, "COD", StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "COD payments are verified when order is completed.");
        }

        if (!_gateway.VerifySignature(request, out var verifyMessage))
        {
            return new BasicResponse(false, verifyMessage);
        }

        payment.Status = request.Status;
        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync(cancellationToken);

        var log = new PaymentLog
        {
            PaymentId = payment.PaymentId,
            RawResponse = request.RawResponse,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.PaymentLogs.AddAsync(log, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogPaymentActionAsync(userId, "PaymentVerified", new { payment.PaymentId, payment.Status }, cancellationToken);
        await UpdateOrderByPaymentAsync(payment, cancellationToken);

        return new BasicResponse(true, "Payment updated.");
    }

    private static bool IsPaymentTransitionAllowed(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return true;
        }
        if (string.Equals(current, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, PaymentStatuses.Failed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, PaymentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private async Task UpdateOrderByPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (!payment.OrderId.HasValue) return;
        var order = await _uow.Orders.GetByIdAsync(payment.OrderId.Value);
        if (order == null) return;
        if (!string.Equals(order.Status, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(payment.Status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
        {
            order.Status = OrderStatuses.Confirmed;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            await _escrowService.FundForOrderAsync(order.OrderId, payment.PaymentId, cancellationToken);
        }
        else if (string.Equals(payment.Status, PaymentStatuses.Failed, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(payment.Status, PaymentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            order.Status = OrderStatuses.Cancelled;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            await _escrowService.RefundForOrderAsync(order.OrderId, cancellationToken);

            if (order.ListingId.HasValue)
            {
                var listing = await _uow.Listings.GetByIdAsync(order.ListingId.Value);
                if (listing != null && string.Equals(listing.Status, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase))
                {
                    listing.Status = ListingStatuses.Active;
                    listing.UpdatedAt = DateTime.UtcNow;
                    _uow.Listings.Update(listing);
                    await _uow.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }

    private async Task LogPaymentActionAsync(long userId, string action, object details, CancellationToken cancellationToken)
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
