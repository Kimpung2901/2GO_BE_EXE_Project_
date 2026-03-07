namespace _2GO_EXE_Project.BAL.DTOs.Payments;

public record CreatePaymentRequest(long OrderId, string Method, string? PaymentStage = null);

public record CreateSubscriptionPaymentRequest(string Method, string PlanCode);

public record VerifyPaymentRequest(string Status, string? RawResponse, string? Signature);

public record PaymentResponse(
    long PaymentId,
    decimal? Amount,
    string? Method,
    string? Status,
    string? ReferenceCode,
    DateTime? CreatedAt,
    string? PayUrl,
    string? PaymentStage);
