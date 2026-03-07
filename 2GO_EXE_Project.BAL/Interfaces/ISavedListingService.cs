using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Listings;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ISavedListingService
{
    Task<SavedListingListResponse> GetMySavedAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default);
    Task<SavedListingStatusResponse> GetSavedStatusAsync(ClaimsPrincipal userPrincipal, long listingId, CancellationToken cancellationToken = default);
    Task<BasicResponse> SaveAsync(ClaimsPrincipal userPrincipal, SaveListingRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> RemoveAsync(ClaimsPrincipal userPrincipal, long listingId, CancellationToken cancellationToken = default);
}
