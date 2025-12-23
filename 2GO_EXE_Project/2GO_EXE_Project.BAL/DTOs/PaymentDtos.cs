namespace _2GO_EXE_Project.BAL.DTOs.Payments;

public record CreatePaymentRequest(long OrderId, string Method);

public record VerifyPaymentRequest(string Status, string? RawResponse, string? Signature);

public record PaymentResponse(
    long PaymentId,
    decimal? Amount,
    string? Method,
    string? Status,
    string? ReferenceCode,
    DateTime? CreatedAt,
    string? PayUrl);
