using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Escrow;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IEscrowService
{
    Task<EscrowResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateEscrowRequest request, CancellationToken cancellationToken = default);
    Task<EscrowResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<EscrowTransactionResponse> AddTransactionAsync(ClaimsPrincipal userPrincipal, long escrowId, CreateEscrowTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> ReleaseAsync(ClaimsPrincipal userPrincipal, long escrowId, CancellationToken cancellationToken = default);
}
