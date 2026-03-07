using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class PayosPaymentGateway : IPayosPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PayOSSettings _settings;

    public PayosPaymentGateway(HttpClient httpClient, IOptions<PayOSSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new PayOSSettings();
    }

    public async Task<PayosCreatePaymentResponse> CreatePaymentAsync(PayosCreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var payload = BuildCreatePayload(request);
        var signature = ComputeSignature(payload, _settings.ChecksumKey!);
        payload["signature"] = signature;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoint);
        httpRequest.Headers.Add("x-client-id", _settings.ClientId);
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Tạo thanh toán PayOS thất bại: {(int)response.StatusCode} {response.ReasonPhrase}. {raw}");
        }

        return ParseCreateResponse(raw);
    }

    public bool VerifyWebhookSignature(PayosWebhookRequest request, out string message)
    {
        if (string.IsNullOrWhiteSpace(_settings.ChecksumKey))
        {
            message = "Chưa cấu hình PayOS.";
            return false;
        }
        if (request == null)
        {
            message = "Thiếu dữ liệu webhook.";
            return false;
        }
        if (request.Data == null)
        {
            message = "Dữ liệu webhook là bắt buộc.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            message = "Signature là bắt buộc.";
            return false;
        }

        var data = BuildWebhookSignatureData(request.Data);
        var computed = ComputeSignature(data, _settings.ChecksumKey);
        var provided = request.Signature.Trim();

        if (!string.Equals(computed, provided, StringComparison.OrdinalIgnoreCase))
        {
            message = "Chữ ký PayOS không hợp lệ.";
            return false;
        }

        message = "Signature verified.";
        return true;
    }

    private Dictionary<string, object?> BuildCreatePayload(PayosCreatePaymentRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["orderCode"] = request.OrderCode,
            ["amount"] = request.Amount,
            ["description"] = request.Description,
            ["returnUrl"] = request.ReturnUrl ?? _settings.ReturnUrl,
            ["cancelUrl"] = request.CancelUrl ?? _settings.CancelUrl
        };

        if (!string.IsNullOrWhiteSpace(request.BuyerName)) payload["buyerName"] = request.BuyerName;
        if (!string.IsNullOrWhiteSpace(request.BuyerEmail)) payload["buyerEmail"] = request.BuyerEmail;
        if (!string.IsNullOrWhiteSpace(request.BuyerPhone)) payload["buyerPhone"] = request.BuyerPhone;

        return payload;
    }

    private static Dictionary<string, object?> BuildWebhookSignatureData(PayosWebhookData data)
    {
        var payload = new Dictionary<string, object?>
        {
            ["orderCode"] = data.OrderCode,
            ["amount"] = data.Amount
        };

        if (!string.IsNullOrWhiteSpace(data.Description)) payload["description"] = data.Description;
        if (!string.IsNullOrWhiteSpace(data.Currency)) payload["currency"] = data.Currency;
        if (!string.IsNullOrWhiteSpace(data.Status)) payload["status"] = data.Status;
        if (!string.IsNullOrWhiteSpace(data.PaymentLinkId)) payload["paymentLinkId"] = data.PaymentLinkId;
        if (!string.IsNullOrWhiteSpace(data.Reference)) payload["reference"] = data.Reference;
        if (!string.IsNullOrWhiteSpace(data.TransactionDateTime)) payload["transactionDateTime"] = data.TransactionDateTime;

        return payload;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ChecksumKey) ||
            string.IsNullOrWhiteSpace(_settings.Endpoint) ||
            string.IsNullOrWhiteSpace(_settings.ReturnUrl) ||
            string.IsNullOrWhiteSpace(_settings.CancelUrl))
        {
            throw new InvalidOperationException("Chưa cấu hình PayOS.");
        }
    }

    private static string ComputeSignature(Dictionary<string, object?> payload, string checksumKey)
    {
        var parts = payload
            .Where(kvp => kvp.Value != null)
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => $"{kvp.Key}={SerializeValue(kvp.Value)}");
        var raw = string.Join("&", parts);

        var keyBytes = Encoding.UTF8.GetBytes(checksumKey);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SerializeValue(object? value)
    {
        if (value == null) return string.Empty;
        return value switch
        {
            string text => text,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static PayosCreatePaymentResponse ParseCreateResponse(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var dataProp) ? dataProp : root;

            var checkoutUrl = TryGetString(data, "checkoutUrl");
            var paymentLinkId = TryGetString(data, "paymentLinkId");
            var orderCode = TryGetLong(data, "orderCode");
            var qrCode = TryGetString(data, "qrCode");
            var status = TryGetString(data, "status");
            var amount = TryGetLong(data, "amount");
            var currency = TryGetString(data, "currency");
            var expiredAt = TryGetDateTime(data, "expiredAt");

            return new PayosCreatePaymentResponse(checkoutUrl, paymentLinkId, orderCode, qrCode, status, amount, currency, expiredAt, raw);
        }
        catch (JsonException)
        {
            return new PayosCreatePaymentResponse(null, null, null, null, null, null, null, null, raw);
        }
    }

    private static string? TryGetString(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
        }
        return null;
    }

    private static long? TryGetLong(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var value)) return value;
            if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var parsed)) return parsed;
        }
        return null;
    }

    private static DateTime? TryGetDateTime(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), out var parsed)) return parsed;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var unix))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }
        return null;
    }
}



