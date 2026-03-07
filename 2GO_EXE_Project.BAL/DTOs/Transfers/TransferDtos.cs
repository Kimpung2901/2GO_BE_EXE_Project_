namespace _2GO_EXE_Project.BAL.DTOs.Transfers;

public record CreateTransferRequest(
    long Amount,
    string Description,
    string ToBin,
    string ToAccountNumber,
    List<string>? Category);

public record TransferResponse(
    long TransferId,
    string ReferenceId,
    string? PayoutId,
    int? TotalCredit,
    List<string>? Category,
    string ApprovalState,
    DateTime CreatedAt,
    List<TransferTransactionResponse> Transactions);

public record TransferTransactionResponse(
    long TransferTransactionId,
    string ReferenceId,
    string? PayoutTransactionId,
    long Amount,
    string Description,
    string ToBin,
    string ToAccountNumber,
    string? ToAccountName,
    string? Reference,
    DateTime? TransactionDatetime,
    string? ErrorMessage,
    string? ErrorCode,
    string State);

public record CreateBatchTransferRequest(
    List<CreateTransferRequest> Transfers);

public record EstimateCreditRequest(
    List<CreateTransferRequest> Transfers);

public record PayoutAccountBalance(
    long Amount,
    string Currency);
