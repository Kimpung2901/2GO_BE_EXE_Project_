using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class MomoPaymentGateway : IMomoPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly MomoSettings _settings;

    public MomoPaymentGateway(HttpClient httpClient, IOptions<MomoSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new MomoSettings();
    }

    public async Task<MomoCreatePaymentResponse> CreatePaymentAsync(MomoCreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var orderId = request.OrderId;
        var requestId = string.IsNullOrWhiteSpace(request.RequestId) ? orderId : request.RequestId!;
        var extraData = request.ExtraData ?? string.Empty;
        var requestType = string.IsNullOrWhiteSpace(_settings.RequestType) ? "captureWallet" : _settings.RequestType!;
        var orderInfo = request.OrderInfo ?? string.Empty;

        var rawSignature = $"accessKey={_settings.AccessKey}" +
                           $"&amount={request.Amount}" +
                           $"&extraData={extraData}" +
                           $"&ipnUrl={_settings.NotifyUrl}" +
                           $"&orderId={orderId}" +
                           $"&orderInfo={orderInfo}" +
                           $"&partnerCode={_settings.PartnerCode}" +
                           $"&redirectUrl={_settings.ReturnUrl}" +
                           $"&requestId={requestId}" +
                           $"&requestType={requestType}";
        var signature = ComputeSignature(rawSignature, _settings.SecretKey!);

        var payload = new
        {
            partnerCode = _settings.PartnerCode,
            partnerName = _settings.PartnerName,
            storeId = _settings.StoreId,
            requestId,
            amount = request.Amount,
            orderId,
            orderInfo,
            redirectUrl = _settings.ReturnUrl,
            ipnUrl = _settings.NotifyUrl,
            extraData,
            requestType,
            lang = string.IsNullOrWhiteSpace(_settings.Language) ? "vi" : _settings.Language,
            signature
        };

        using var response = await _httpClient.PostAsJsonAsync(_settings.Endpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"MoMo create payment failed: {(int)response.StatusCode} {response.ReasonPhrase}. {raw}");
        }

        return ParseCreateResponse(raw);
    }

    public bool VerifyIpnSignature(MomoIpnRequest request, out string message)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(_settings.AccessKey))
        {
            message = "MoMo settings are not configured.";
            return false;
        }
        if (request == null)
        {
            message = "IPN request is missing.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            message = "Signature is required.";
            return false;
        }

        var rawSignature = $"accessKey={_settings.AccessKey}" +
                           $"&amount={request.Amount}" +
                           $"&extraData={request.ExtraData ?? string.Empty}" +
                           $"&message={request.Message ?? string.Empty}" +
                           $"&orderId={request.OrderId ?? string.Empty}" +
                           $"&orderInfo={request.OrderInfo ?? string.Empty}" +
                           $"&orderType={request.OrderType ?? string.Empty}" +
                           $"&partnerCode={request.PartnerCode ?? string.Empty}" +
                           $"&payType={request.PayType ?? string.Empty}" +
                           $"&requestId={request.RequestId ?? string.Empty}" +
                           $"&responseTime={request.ResponseTime}" +
                           $"&resultCode={request.ResultCode}" +
                           $"&transId={request.TransId}";
        var computed = ComputeSignature(rawSignature, _settings.SecretKey);
        var provided = request.Signature.Trim().ToLowerInvariant();

        if (!string.Equals(computed, provided, StringComparison.OrdinalIgnoreCase))
        {
            message = "Invalid MoMo signature.";
            return false;
        }

        message = "Signature verified.";
        return true;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.PartnerCode) ||
            string.IsNullOrWhiteSpace(_settings.AccessKey) ||
            string.IsNullOrWhiteSpace(_settings.SecretKey) ||
            string.IsNullOrWhiteSpace(_settings.Endpoint) ||
            string.IsNullOrWhiteSpace(_settings.ReturnUrl) ||
            string.IsNullOrWhiteSpace(_settings.NotifyUrl))
        {
            throw new InvalidOperationException("MoMo is not configured.");
        }
    }

    private static string ComputeSignature(string raw, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static MomoCreatePaymentResponse ParseCreateResponse(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var payUrl = root.TryGetProperty("payUrl", out var payUrlProp) ? payUrlProp.GetString() : null;
            var requestId = root.TryGetProperty("requestId", out var requestIdProp) ? requestIdProp.GetString() : null;
            var orderId = root.TryGetProperty("orderId", out var orderIdProp) ? orderIdProp.GetString() : null;
            var resultCode = root.TryGetProperty("resultCode", out var resultCodeProp) ? resultCodeProp.GetInt32() : -1;
            var message = root.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : null;
            return new MomoCreatePaymentResponse(payUrl, requestId, orderId, resultCode, message, raw);
        }
        catch (JsonException)
        {
            return new MomoCreatePaymentResponse(null, null, null, -1, "Invalid MoMo response.", raw);
        }
    }
}
