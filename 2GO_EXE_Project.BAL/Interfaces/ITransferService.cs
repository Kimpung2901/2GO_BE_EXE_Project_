using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Transfers;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ITransferService
{
    Task<TransferResponse> CreateTransferAsync(ClaimsPrincipal userPrincipal, CreateTransferRequest request, CancellationToken cancellationToken = default);
    Task<TransferResponse> CreateSystemTransferAsync(long userId, CreateTransferRequest request, CancellationToken cancellationToken = default);
    Task<TransferResponse> CreateBatchTransferAsync(ClaimsPrincipal userPrincipal, CreateBatchTransferRequest request, CancellationToken cancellationToken = default);
    Task<TransferResponse?> GetTransferByIdAsync(ClaimsPrincipal userPrincipal, long transferId, CancellationToken cancellationToken = default);
    Task<TransferResponse?> GetTransferByReferenceIdAsync(ClaimsPrincipal userPrincipal, string referenceId, CancellationToken cancellationToken = default);
    Task<List<TransferResponse>> GetUserTransfersAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<object> GetAccountBalanceAsync(CancellationToken cancellationToken = default);
    Task<object> EstimateCreditAsync(EstimateCreditRequest request, CancellationToken cancellationToken = default);
}
