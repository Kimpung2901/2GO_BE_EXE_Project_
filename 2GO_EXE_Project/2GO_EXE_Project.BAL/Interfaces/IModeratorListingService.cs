using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IModeratorListingService
{
    Task<ListingListResponse> GetListingsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<ListingDetail?> GetByIdAsync(long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> ApproveAsync(ClaimsPrincipal modPrincipal, long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> RejectAsync(ClaimsPrincipal modPrincipal, long listingId, RejectListingRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> FlagAsync(ClaimsPrincipal modPrincipal, long listingId, FlagListingRequest request, CancellationToken cancellationToken = default);
}
