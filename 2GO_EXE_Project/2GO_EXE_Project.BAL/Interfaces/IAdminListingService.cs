using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminListingService
{
    Task<ListingListResponse> GetListingsAsync(string? status, long? sellerId, int? categoryId, int? subCategoryId, int skip, int take, CancellationToken cancellationToken = default);
    Task<ListingDetail?> GetByIdAsync(long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal adminPrincipal, long listingId, UpdateListingStatusRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> DeleteAsync(ClaimsPrincipal adminPrincipal, long listingId, CancellationToken cancellationToken = default); // soft delete
}
