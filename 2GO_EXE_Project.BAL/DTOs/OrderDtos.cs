namespace _2GO_EXE_Project.BAL.DTOs.Orders;

public record CreateOrderRequest(long ListingId, string PaymentMethod, string DeliveryAddress, string? DeliveryPhone = null);

public record OrderResponse(
    long OrderId,
    long ListingId,
    long BuyerId,
    long SellerId,
    long? EscrowId,
    long OrderCode,
    string PaymentLinkId,
    decimal? TotalAmount,
    string? PaymentMethod,
    string? Status,
    string? CheckoutUrl,
    string? QrCodeUrl,
    DateTime? PaymentExpiredAt,
    DateTime? CreatedAt,
    string? DeliveryAddress,
    string? DeliveryPhone,
    decimal? DepositAmount,
    DateTime? DepositDeadlineAt,
    bool DepositRequired,
    bool DepositPaid,
    string? EscrowStatus);

public record OrderListItem(
    long OrderId,
    long ListingId,
    long BuyerId,
    long SellerId,
    long OrderCode,
    string PaymentLinkId,
    decimal? TotalAmount,
    string? PaymentMethod,
    string? Status,
    string? CheckoutUrl,
    string? QrCodeUrl,
    DateTime? PaymentExpiredAt,
    DateTime? CreatedAt,
    string? ListingTitle,
    decimal? ListingPrice,
    string? DeliveryAddress,
    bool DepositRequired,
    bool DepositPaid);

public record OrderListResponse(int Total, IReadOnlyList<OrderListItem> Items);

public record OrderDetailResponse(
    long OrderId,
    long ListingId,
    long BuyerId,
    long SellerId,
    long? EscrowId,
    long OrderCode,
    string PaymentLinkId,
    decimal? TotalAmount,
    string? PaymentMethod,
    string? Status,
    string? CheckoutUrl,
    string? QrCodeUrl,
    DateTime? PaymentExpiredAt,
    DateTime? CreatedAt,
    string? ListingTitle,
    decimal? ListingPrice,
    string? BuyerEmail,
    string? BuyerPhone,
    string? SellerEmail,
    string? SellerPhone,
    string? DeliveryAddress,
    string? DeliveryPhone,
    decimal? DepositAmount,
    DateTime? DepositDeadlineAt,
    bool DepositRequired,
    bool DepositPaid,
    string? EscrowStatus);

public record UpdateOrderStatusRequest(string Status, string? Reason);
