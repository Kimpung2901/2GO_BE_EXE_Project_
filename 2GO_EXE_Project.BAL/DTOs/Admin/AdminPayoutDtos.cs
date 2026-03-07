namespace _2GO_EXE_Project.BAL.DTOs.Admin;

public record AdminPayoutItem(
    long EscrowId,
    long? OrderId,
    long? SellerId,
    string? SellerName,
    decimal? Amount,
    string Status,
    DateTime? CreatedAt);

public record AdminPayoutListResponse(int Total, IReadOnlyList<AdminPayoutItem> Items);

public record RetryPayoutRequest(long EscrowId);
