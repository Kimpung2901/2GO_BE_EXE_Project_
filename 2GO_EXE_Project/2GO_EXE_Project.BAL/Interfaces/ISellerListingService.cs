using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ISellerListingService
{
    Task<SellerListingListResponse> GetMyListingsAsync(ClaimsPrincipal sellerPrincipal, string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<ListingDetail?> GetMyListingByIdAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default);
    Task<ListingDetail> CreateAsync(ClaimsPrincipal sellerPrincipal, CreateSellerListingRequest request, CancellationToken cancellationToken = default);
    Task<ListingDetail?> UpdateAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateSellerListingRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> PublishAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> ArchiveAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateImagesAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateListingImagesRequest request, CancellationToken cancellationToken = default);
    Task<ListingStatsResponse?> GetMyListingStatsAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default);
}
