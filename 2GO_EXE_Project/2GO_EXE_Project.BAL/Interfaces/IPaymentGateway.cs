using _2GO_EXE_Project.BAL.DTOs.Payments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPaymentGateway
{
    bool VerifySignature(VerifyPaymentRequest request, out string message);
}
