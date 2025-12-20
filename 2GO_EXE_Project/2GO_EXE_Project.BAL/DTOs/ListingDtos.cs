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

public record ListingDetail(
    long ListingId,
    string? Title,
    string? Description,
    decimal? Price,
    bool? HasNegotiation,
    string? Condition,
    string? Brand,
    string? Status,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    int? CategoryId,
    int? SubCategoryId,
    string? CategoryName,
    string? SubCategoryName,
    string? SellerEmail,
    string? SellerPhone,
    string? PrimaryImageUrl,
    IReadOnlyList<string> Images);

public record ListingListResponse(int Total, IReadOnlyList<ListingListItem> Items);

public record RejectListingRequest(string Reason);
public record FlagListingRequest(string Reason);
public record UpdateListingStatusRequest(string Status);
