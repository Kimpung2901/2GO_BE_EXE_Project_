using _2GO_EXE_Project.BAL.DTOs.Payments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPayosPaymentGateway
{
    Task<PayosCreatePaymentResponse> CreatePaymentAsync(PayosCreatePaymentRequest request, CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(PayosWebhookRequest request, out string message);
}
