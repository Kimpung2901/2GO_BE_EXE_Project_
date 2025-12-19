using _2GO_EXE_Project.BAL.DTOs.Listings;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IListingService
{
    Task<ListingListResponse> GetListingsAsync(string? search, int? categoryId, int? subCategoryId, decimal? minPrice, decimal? maxPrice, string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<ListingDetail?> GetListingByIdAsync(long listingId, bool onlyActive, CancellationToken cancellationToken = default);
}
