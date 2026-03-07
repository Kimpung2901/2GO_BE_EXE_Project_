using System.Text.Json;

namespace _2GO_EXE_Project.BAL.DTOs.Carts;

public record AddCartItemRequest(
    long ListingId,
    int Quantity,
    long? VariantId = null,
    JsonElement? VariantSnapshot = null,
    string? Note = null);

public record UpdateCartItemRequest(
    int? Quantity = null,
    string? Note = null,
    bool? IsSelected = null);

public record CartItemResponse(
    long CartItemId,
    long ListingId,
    long SellerId,
    long? VariantId,
    int Quantity,
    decimal? PriceSnapshot,
    decimal? OriginalPrice,
    string? Currency,
    string? VariantSnapshot,
    string? Note,
    bool IsSelected,
    string? Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

public record CartSellerGroupResponse(
    long SellerId,
    IReadOnlyList<CartItemResponse> Items);

public record CartResponse(
    long CartId,
    string? Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CartSellerGroupResponse> Groups);

public record CheckoutCartRequest(string PaymentMethod);

public record CartValidationError(long CartItemId, string Message);

public record CheckoutOrderSummary(
    long OrderId,
    long SellerId,
    decimal? TotalAmount,
    string? PaymentMethod,
    string? Status,
    IReadOnlyList<long> ListingIds);

public record CheckoutCartResponse(
    bool Success,
    string? Message,
    IReadOnlyList<CartValidationError> Errors,
    IReadOnlyList<CheckoutOrderSummary> Orders);
