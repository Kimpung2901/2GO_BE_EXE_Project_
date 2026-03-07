using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayOS;
using PayOS.Models.V1.Payouts;
using PayOS.Models.V1.Payouts.Batch;
using _2GO_EXE_Project.BAL.DTOs.Transfers;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class TransferService : ITransferService
{
    private readonly IUnitOfWork _uow;
    private readonly PayOSClient _payosClient;

    public TransferService(IUnitOfWork uow, [FromKeyedServices("TransferClient")] PayOSClient payosClient)
    {
        _uow = uow;
        _payosClient = payosClient;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    private static DateTime? ParseTransactionDateTime(string? dateTimeString)
    {
        if (string.IsNullOrWhiteSpace(dateTimeString))
            return null;

        // Try parsing with DateTimeOffset first to preserve timezone info
        if (DateTimeOffset.TryParse(dateTimeString, out var dateTimeOffset))
        {
            return dateTimeOffset.UtcDateTime;
        }

        // Fallback: Try parsing as DateTime and assume UTC
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            // If parsed DateTime is Local, convert to UTC
            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            // If Unspecified, assume it's UTC
            else if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            return dateTime;
        }

        return null;
    }

    public async Task<TransferResponse> CreateTransferAsync(ClaimsPrincipal userPrincipal, CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        return await CreateTransferCoreAsync(userId, request, cancellationToken);
    }

    public async Task<TransferResponse> CreateSystemTransferAsync(long userId, CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateTransferCoreAsync(userId, request, cancellationToken);
    }

    private async Task<TransferResponse> CreateTransferCoreAsync(long userId, CreateTransferRequest request, CancellationToken cancellationToken)
    {

        var referenceId = Guid.NewGuid().ToString();
        var payoutRequest = new PayoutRequest
        {
            ReferenceId = referenceId,
            Amount = request.Amount,
            Description = request.Description,
            ToBin = request.ToBin,
            ToAccountNumber = request.ToAccountNumber,
            Category = request.Category ?? new List<string>()
        };

        try
        {
            var payoutResponse = await _payosClient.Payouts.CreateAsync(payoutRequest);

                var transfer = new Transfer
                {
                    ReferenceId = referenceId,
                    PayoutId = payoutResponse.Id,
                    UserId = userId,
                Category = request.Category != null ? JsonSerializer.Serialize(request.Category) : null,
                ApprovalState = payoutResponse.ApprovalState.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Transfers.AddAsync(transfer, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // Add transactions
            foreach (var transaction in payoutResponse.Transactions)
            {
                var transferTransaction = new TransferTransaction
                {
                    TransferId = transfer.TransferId,
                    ReferenceId = transaction.ReferenceId,
                    PayoutTransactionId = transaction.Id,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    ToBin = transaction.ToBin,
                    ToAccountNumber = transaction.ToAccountNumber,
                    ToAccountName = transaction.ToAccountName,
                    Reference = transaction.Reference,
                    TransactionDatetime = ParseTransactionDateTime(transaction.TransactionDatetime),
                    ErrorMessage = transaction.ErrorMessage,
                    ErrorCode = transaction.ErrorCode,
                    State = transaction.State.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.TransferTransactions.AddAsync(transferTransaction, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return await MapToTransferResponse(transfer, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Tạo chuyển khoản thất bại: {ex.Message}", ex);
        }
    }

    public async Task<TransferResponse> CreateBatchTransferAsync(ClaimsPrincipal userPrincipal, CreateBatchTransferRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);

        var referenceId = Guid.NewGuid().ToString();
        var batchPayoutRequest = new PayoutBatchRequest
        {
            ReferenceId = referenceId,
            Payouts = [.. request.Transfers.Select(r => new PayoutBatchItem
            {
                ReferenceId = Guid.NewGuid().ToString(),
                Amount = r.Amount,
                Description = r.Description,
                ToBin = r.ToBin,
                ToAccountNumber = r.ToAccountNumber
            })],
            Category = [.. request.Transfers.SelectMany(r => r.Category ?? new List<string>()).Distinct()]
        };

        try
        {
            var payoutResponse = await _payosClient.Payouts.Batch.CreateAsync(batchPayoutRequest);

            var transfer = new Transfer
            {
                ReferenceId = referenceId,
                PayoutId = payoutResponse.Id,
                UserId = userId,
                Category = JsonSerializer.Serialize(payoutResponse.Category),
                ApprovalState = payoutResponse.ApprovalState.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Transfers.AddAsync(transfer, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // Add transactions
            foreach (var transaction in payoutResponse.Transactions)
            {
                var transferTransaction = new TransferTransaction
                {
                    TransferId = transfer.TransferId,
                    ReferenceId = transaction.ReferenceId,
                    PayoutTransactionId = transaction.Id,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    ToBin = transaction.ToBin,
                    ToAccountNumber = transaction.ToAccountNumber,
                    ToAccountName = transaction.ToAccountName,
                    Reference = transaction.Reference,
                    TransactionDatetime = ParseTransactionDateTime(transaction.TransactionDatetime),
                    ErrorMessage = transaction.ErrorMessage,
                    ErrorCode = transaction.ErrorCode,
                    State = transaction.State.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.TransferTransactions.AddAsync(transferTransaction, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return await MapToTransferResponse(transfer, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Tạo chuyển khoản hàng loạt thất bại: {ex.Message}", ex);
        }
    }

    public async Task<TransferResponse?> GetTransferByIdAsync(ClaimsPrincipal userPrincipal, long transferId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);

        var transfer = await _uow.Transfers.Query()
            .Include(t => t.Transactions)
            .FirstOrDefaultAsync(t => t.TransferId == transferId && t.UserId == userId, cancellationToken);

        if (transfer == null)
            return null;

        // Sync with PayOS
        await SyncTransferWithPayOSAsync(transfer, cancellationToken);

        return await MapToTransferResponse(transfer, cancellationToken);
    }

    public async Task<TransferResponse?> GetTransferByReferenceIdAsync(ClaimsPrincipal userPrincipal, string referenceId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);

        var transfer = await _uow.Transfers.Query()
            .Include(t => t.Transactions)
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId && t.UserId == userId, cancellationToken);

        if (transfer == null)
            return null;

        // Sync with PayOS
        await SyncTransferWithPayOSAsync(transfer, cancellationToken);

        return await MapToTransferResponse(transfer, cancellationToken);
    }

    public async Task<List<TransferResponse>> GetUserTransfersAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);

        var transfers = await _uow.Transfers.Query()
            .Include(t => t.Transactions)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var responses = new List<TransferResponse>();
        foreach (var transfer in transfers)
        {
            responses.Add(await MapToTransferResponse(transfer, cancellationToken));
        }

        return responses;
    }

    public async Task<object> GetAccountBalanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var balance = await _payosClient.PayoutsAccount.GetBalanceAsync();
            return balance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lấy số dư tài khoản thất bại: {ex.Message}", ex);
        }
    }

    public async Task<object> EstimateCreditAsync(EstimateCreditRequest request, CancellationToken cancellationToken = default)
    {
        var batchPayoutRequest = new PayoutBatchRequest
        {
            ReferenceId = Guid.NewGuid().ToString(),
            Payouts = [.. request.Transfers.Select(r => new PayoutBatchItem
            {
                ReferenceId = Guid.NewGuid().ToString(),
                Amount = r.Amount,
                Description = r.Description,
                ToBin = r.ToBin,
                ToAccountNumber = r.ToAccountNumber
            })],
            Category = [.. request.Transfers.SelectMany(r => r.Category ?? new List<string>()).Distinct()]
        };

        try
        {
            var result = await _payosClient.Payouts.EstimateCredit(batchPayoutRequest);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ước tính hạn mức thất bại: {ex.Message}", ex);
        }
    }

    private async Task SyncTransferWithPayOSAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        try
        {
            Payout payout;
            if (string.IsNullOrEmpty(transfer.PayoutId))
            {
                var payoutPage = await _payosClient.Payouts.ListAsync(new GetPayoutListParam { ReferenceId = transfer.ReferenceId });
                if (payoutPage.Data.Count > 0)
                {
                    payout = payoutPage.Data[0];
                    transfer.PayoutId = payout.Id;
                }
                else
                {
                    return;
                }
            }
            else
            {
                payout = await _payosClient.Payouts.GetAsync(transfer.PayoutId);
            }

            transfer.ApprovalState = payout.ApprovalState.ToString();
            transfer.UpdatedAt = DateTime.UtcNow;

            // Update transactions
            foreach (var payoutTransaction in payout.Transactions)
            {
                var existingTransaction = await _uow.TransferTransactions.Query()
                    .FirstOrDefaultAsync(t => t.ReferenceId == payoutTransaction.ReferenceId && t.TransferId == transfer.TransferId, cancellationToken);

                if (existingTransaction != null)
                {
                    existingTransaction.PayoutTransactionId = payoutTransaction.Id;
                    existingTransaction.ToAccountName = payoutTransaction.ToAccountName;
                    existingTransaction.Reference = payoutTransaction.Reference;
                    existingTransaction.TransactionDatetime = ParseTransactionDateTime(payoutTransaction.TransactionDatetime);
                    existingTransaction.ErrorMessage = payoutTransaction.ErrorMessage;
                    existingTransaction.ErrorCode = payoutTransaction.ErrorCode;
                    existingTransaction.State = payoutTransaction.State.ToString();

                    _uow.TransferTransactions.Update(existingTransaction);
                }
            }

            _uow.Transfers.Update(transfer);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Ignore sync errors
        }
    }

    private async Task<TransferResponse> MapToTransferResponse(Transfer transfer, CancellationToken cancellationToken)
    {
        var transactions = await _uow.TransferTransactions.Query()
            .Where(t => t.TransferId == transfer.TransferId)
            .ToListAsync(cancellationToken);

        var category = !string.IsNullOrEmpty(transfer.Category)
            ? JsonSerializer.Deserialize<List<string>>(transfer.Category)
            : null;

        return new TransferResponse(
            transfer.TransferId,
            transfer.ReferenceId,
            transfer.PayoutId,
            transfer.TotalCredit,
            category,
            transfer.ApprovalState,
            transfer.CreatedAt,
            transactions.Select(t => new TransferTransactionResponse(
                t.TransferTransactionId,
                t.ReferenceId,
                t.PayoutTransactionId,
                t.Amount,
                t.Description,
                t.ToBin,
                t.ToAccountNumber,
                t.ToAccountName,
                t.Reference,
                t.TransactionDatetime,
                t.ErrorMessage,
                t.ErrorCode,
                t.State
            )).ToList()
        );
    }
}


