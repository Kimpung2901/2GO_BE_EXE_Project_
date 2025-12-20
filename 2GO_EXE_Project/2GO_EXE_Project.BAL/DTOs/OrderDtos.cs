namespace _2GO_EXE_Project.BAL.DTOs.Orders;

public record CreateOrderRequest(long ListingId);

public record OrderResponse(
    long OrderId,
    long ListingId,
    long BuyerId,
    long SellerId,
    decimal? TotalAmount,
    string? Status,
    DateTime? CreatedAt);
