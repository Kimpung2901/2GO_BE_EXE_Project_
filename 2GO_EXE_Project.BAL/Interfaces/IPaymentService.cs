using System.Security.Claims;
using PayOS.Models.Webhooks;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Payments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreateSubscriptionAsync(ClaimsPrincipal userPrincipal, CreateSubscriptionPaymentRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> VerifyAsync(ClaimsPrincipal userPrincipal, long paymentId, VerifyPaymentRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> HandlePayosWebhookAsync(PayosWebhookRequest request, CancellationToken cancellationToken = default);
    Task<WebhookData> VerifyWebhookSignatureAsync(Webhook webhook, CancellationToken cancellationToken = default);
    Task<BasicResponse> HandlePayOSWebhookAsync(Webhook webhook, CancellationToken cancellationToken = default);
}
