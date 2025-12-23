namespace _2GO_EXE_Project.BAL.DTOs.Escrow;

public record CreateEscrowRequest(long OrderId, decimal DepositAmount);

public record EscrowResponse(
    long EscrowId,
    long OrderId,
    long BuyerId,
    long SellerId,
    decimal? DepositAmount,
    decimal? TotalAmount,
    string? Status,
    DateTime? CreatedAt);

public record CreateEscrowTransactionRequest(string Type, string Method, decimal Amount);

public record EscrowTransactionResponse(
    long TxId,
    long EscrowId,
    string? Type,
    string? Method,
    decimal? Amount,
    string? Status,
    DateTime? CreatedAt);
