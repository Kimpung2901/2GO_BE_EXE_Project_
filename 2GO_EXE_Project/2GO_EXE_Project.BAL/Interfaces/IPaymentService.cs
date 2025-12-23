using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Payments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> VerifyAsync(ClaimsPrincipal userPrincipal, long paymentId, VerifyPaymentRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> HandleMomoIpnAsync(MomoIpnRequest request, CancellationToken cancellationToken = default);
}
