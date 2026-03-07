namespace _2GO_EXE_Project.BAL.DTOs.Listings;

public record SaveListingRequest(long ListingId);

public record SavedListingItem(
    long ListingId,
    DateTime? SavedAt,
    string? Title,
    decimal? Price,
    string? PrimaryImageUrl);

public record SavedListingListResponse(int Total, IReadOnlyList<SavedListingItem> Items);

public record SavedListingStatusResponse(long ListingId, bool IsSaved);
