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

    public PaymentService(IUnitOfWork uow, IPaymentGateway gateway)
    {
        _uow = uow;
        _gateway = gateway;
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
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than 0.");
        }

        var payment = new Payment
        {
            UserId = userId,
            Amount = request.Amount,
            Method = request.Method,
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

        return new BasicResponse(true, "Payment updated.");
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
