using PayOS.Models.Webhooks;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPayOSService
{
    /// <summary>
    /// Create a PayOS payment link
    /// </summary>
    Task<(string PaymentUrl, string PayOSOrderCode)> CreatePaymentLinkAsync(
        long paymentId,
        string referenceCode,
        long amount,
        string description,
        string? returnUrl = null,
        string? cancelUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a payment link
    /// </summary>
    Task<BasicResponse> CancelPaymentLinkAsync(long orderCode, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment link information
    /// </summary>
    Task<object?> GetPaymentLinkInfoAsync(long orderCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify webhook signature from PayOS and return verified webhook data
    /// </summary>
    Task<WebhookData?> VerifyWebhookSignatureAsync(Webhook webhook, CancellationToken cancellationToken = default);
}
