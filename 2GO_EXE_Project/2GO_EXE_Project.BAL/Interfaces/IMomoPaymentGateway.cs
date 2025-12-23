using _2GO_EXE_Project.BAL.DTOs.Payments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IMomoPaymentGateway
{
    Task<MomoCreatePaymentResponse> CreatePaymentAsync(MomoCreatePaymentRequest request, CancellationToken cancellationToken = default);
    bool VerifyIpnSignature(MomoIpnRequest request, out string message);
}
