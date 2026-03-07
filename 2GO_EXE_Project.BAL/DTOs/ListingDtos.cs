namespace _2GO_EXE_Project.BAL.DTOs.Listings;

public record ListingListItem(
    long ListingId,
    string? Title,
    decimal? Price,
    string? Status,
    DateTime? CreatedAt,
    int? CategoryId,
    int? SubCategoryId,
    string? CategoryName,
    string? SubCategoryName,
    string? PrimaryImageUrl);

public record ListingAttributeItem(string Name, string Value);

public record ListingMediaItem(string Url, string MediaType, bool IsPrimary, int? SortOrder);

public record ListingDetail(
    long ListingId,
    string? Title,
    string? Description,
    decimal? Price,
    bool? HasNegotiation,
    string? ListingType,
    int? AvailableQuantity,
    string? Condition,
    string? Brand,
    string? Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    int? CategoryId,
    int? SubCategoryId,
    string? CategoryName,
    string? SubCategoryName,
    long? SellerId,
    string? SellerName,
    string? SellerAvatarUrl,
    string? SellerEmail,
    string? SellerPhone,
    string? PrimaryImageUrl,
    IReadOnlyList<ListingMediaItem> Media,
    IReadOnlyList<ListingAttributeItem> Attributes,
    string? WardName,
    string? DistrictName);



public record ListingListResponse(int Total, IReadOnlyList<ListingListItem> Items);

public record RejectListingRequest(string Reason);
public record FlagListingRequest(string Reason);
public record UpdateListingStatusRequest(string Status);

