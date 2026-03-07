namespace _2GO_EXE_Project.BAL.DTOs.Listings;

public record SellerListingListItem(
    long ListingId,
    string? Title,
    decimal? Price,
    string? Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    string? PrimaryImageUrl,
    string? ActiveOrderPaymentMethod,
    bool? ActiveOrderDepositPaid);

public record SellerListingListResponse(int Total, IReadOnlyList<SellerListingListItem> Items);

public record ListingMediaRequest(string Url, string MediaType, bool IsPrimary, int? SortOrder);
public record ListingAttributeRequest(string Name, string Value);

public record CreateSellerListingRequest(
    string Title,
    string? Description,
    int SubCategoryId,
    int? WardId,
    decimal? Price,
    string? ListingType,
    int? AvailableQuantity,
    bool? HasNegotiation,
    string? Condition,
    string? Brand,
    string? Dimensions,
    double? Weight,
    IReadOnlyList<ListingMediaRequest>? Media,
    IReadOnlyList<ListingAttributeRequest>? Attributes,
    string? Status);

public record UpdateSellerListingRequest(
    string? Title,
    string? Description,
    int? SubCategoryId,
    int? WardId,
    decimal? Price,
    string? ListingType,
    int? AvailableQuantity,
    bool? HasNegotiation,
    string? Condition,
    string? Brand,
    string? Dimensions,
    double? Weight,
    IReadOnlyList<ListingAttributeRequest>? Attributes);

public record UpdateListingMediaRequest(IReadOnlyList<ListingMediaRequest> Media);
