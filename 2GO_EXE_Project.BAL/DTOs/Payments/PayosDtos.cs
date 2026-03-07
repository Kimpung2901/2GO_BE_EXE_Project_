namespace _2GO_EXE_Project.BAL.DTOs.Payments;

public record PayosCreatePaymentRequest(
    long OrderCode,
    long Amount,
    string Description,
    string? ReturnUrl = null,
    string? CancelUrl = null,
    string? BuyerName = null,
    string? BuyerEmail = null,
    string? BuyerPhone = null);

public record PayosCreatePaymentResponse(
    string? CheckoutUrl,
    string? PaymentLinkId,
    long? OrderCode,
    string? QrCode,
    string? Status,
    long? Amount,
    string? Currency,
    DateTime? ExpiredAt,
    string? RawResponse);

public class PayosWebhookData
{
    public long OrderCode { get; set; }
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
}

public class PayosWebhookRequest
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public PayosWebhookData? Data { get; set; }
    public string? Signature { get; set; }
}
