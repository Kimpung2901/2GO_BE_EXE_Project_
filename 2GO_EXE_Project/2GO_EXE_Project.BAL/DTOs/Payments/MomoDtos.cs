namespace _2GO_EXE_Project.BAL.DTOs.Payments;

public record MomoCreatePaymentRequest(
    string OrderId,
    long Amount,
    string OrderInfo,
    string? RequestId = null,
    string? ExtraData = null);

public record MomoCreatePaymentResponse(
    string? PayUrl,
    string? RequestId,
    string? OrderId,
    int ResultCode,
    string? Message,
    string? RawResponse);

public class MomoIpnRequest
{
    public string? PartnerCode { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public long Amount { get; set; }
    public string? OrderInfo { get; set; }
    public string? OrderType { get; set; }
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
    public string? PayType { get; set; }
    public long ResponseTime { get; set; }
    public string? ExtraData { get; set; }
    public string? Signature { get; set; }
}
