using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;
using Microsoft.Extensions.Configuration;

namespace _2GO_EXE_Project.BAL.Services;

public class PayOSService : IPayOSService
{
    private readonly PayOSClient _client;
    private readonly PayOSSettings _settings;

    private readonly IConfiguration configuration;

    public PayOSService([FromKeyedServices("OrderClient")] PayOSClient client, IOptions<PayOSSettings> options, IConfiguration configuration)
    {
        _client = client;
        _settings = options.Value ?? new PayOSSettings();
        this.configuration = configuration;
    }

    public async Task<(string PaymentUrl, string PayOSOrderCode)> CreatePaymentLinkAsync(
        long paymentId,
        string referenceCode,
        long amount,
        string description,
        string? returnUrl = null,
        string? cancelUrl = null,
        CancellationToken cancellationToken = default)
    {   
        var frontendBaseUrl = configuration.GetValue<string>("FrontendBaseUrl") ?? Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5173";
        if (amount <= 0)
        {
            throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(referenceCode))
        {
            throw new ArgumentException("Reference code là bắt buộc.", nameof(referenceCode));
        }

        // Generate unique order code from timestamp + random to avoid duplicates
        var orderCode = long.Parse(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() + Random.Shared.Next(100, 999).ToString());

        // PayOS requires description <= 25 characters
        var desc = description ?? $"Payment {referenceCode}";
        if (desc.Length > 25) desc = desc[..25];

        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = desc,
            ReturnUrl = returnUrl ?? $"{frontendBaseUrl}/payment/result", 
            CancelUrl = cancelUrl ?? $"{frontendBaseUrl}/payment/result",
            Items = new List<PaymentLinkItem>
            {
                new PaymentLinkItem
                {
                    Name = desc,
                    Quantity = 1,
                    Price = amount
                }
            }
        };

        try
        {
            var response = await _client.PaymentRequests.CreateAsync(paymentRequest);

            return (response.CheckoutUrl ?? string.Empty, orderCode.ToString());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Tạo liên kết thanh toán PayOS thất bại: {ex.Message}", ex);
        }
    }

    public async Task<BasicResponse> CancelPaymentLinkAsync(long orderCode, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentLinkInfo = await _client.PaymentRequests.GetAsync(orderCode.ToString());

            if (paymentLinkInfo == null)
            {
                return new BasicResponse(false, "Không tìm thấy link thanh toán.");
            }

            // Only cancel if payment is still pending
            if (paymentLinkInfo.Status != PayOS.Models.V2.PaymentRequests.PaymentLinkStatus.Pending)
            {
                return new BasicResponse(false, $"Không thể hủy thanh toán với trạng thái: {paymentLinkInfo.Status}");
            }

            await _client.PaymentRequests.CancelAsync(orderCode.ToString(), reason);

            return new BasicResponse(true, "Đã hủy link thanh toán thành công.");
        }
        catch (Exception ex)
        {
            return new BasicResponse(false, $"Hủy link thanh toán thất bại: {ex.Message}");
        }
    }

    public async Task<object?> GetPaymentLinkInfoAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentLinkInfo = await _client.PaymentRequests.GetAsync(orderCode.ToString());
            return paymentLinkInfo;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lấy thông tin liên kết thanh toán thất bại: {ex.Message}", ex);
        }
    }

    public async Task<WebhookData?> VerifyWebhookSignatureAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        if (webhook == null || webhook.Data == null)
        {
            throw new ArgumentException("Webhook or webhook data is null.");
        }

        try
        {
            // Verify signature using PayOS SDK and return verified data directly
            var verifiedData = await _client.Webhooks.VerifyAsync(webhook);
            return verifiedData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Xác thực chữ ký webhook thất bại: {ex.Message}", ex);
        }
    }
}






