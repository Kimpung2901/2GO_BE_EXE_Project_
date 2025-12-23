using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class HmacPaymentGateway : IPaymentGateway
{
    private readonly PaymentGatewaySettings _settings;

    public HmacPaymentGateway(IOptions<PaymentGatewaySettings> options)
    {
        _settings = options.Value ?? new PaymentGatewaySettings();
    }

    public bool VerifySignature(VerifyPaymentRequest request, out string message)
    {
        if (string.IsNullOrWhiteSpace(_settings.Secret))
        {
            message = "Payment gateway is not configured.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(request.RawResponse) || string.IsNullOrWhiteSpace(request.Signature))
        {
            message = "RawResponse and Signature are required.";
            return false;
        }

        var secretBytes = Encoding.UTF8.GetBytes(_settings.Secret);
        using var hmac = new HMACSHA256(secretBytes);
        var payloadBytes = Encoding.UTF8.GetBytes(request.RawResponse);
        var hash = hmac.ComputeHash(payloadBytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        var provided = request.Signature.Trim().ToLowerInvariant();

        if (!string.Equals(computed, provided, StringComparison.OrdinalIgnoreCase))
        {
            message = "Invalid signature.";
            return false;
        }

        message = "Signature verified.";
        return true;
    }
}
