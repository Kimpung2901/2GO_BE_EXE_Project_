using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class SavedListingService : ISavedListingService
{
    private readonly IUnitOfWork _uow;

    public SavedListingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }
        return id;
    }

    public async Task<SavedListingListResponse> GetMySavedAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var query = _uow.SavedListings.Query()
            .Include(s => s.Listing)
            .ThenInclude(l => l.ListingImages)
            .Where(s => s.UserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.SavedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(s => new SavedListingItem(
                s.ListingId ?? 0,
                s.SavedAt,
                s.Listing != null ? s.Listing.Title : null,
                s.Listing != null ? s.Listing.Price : null,
                s.Listing != null
                    ? s.Listing.ListingImages.OrderByDescending(i => i.IsPrimary == true).ThenBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault()
                    : null))
            .ToListAsync(cancellationToken);

        return new SavedListingListResponse(total, items);
    }

    public async Task<SavedListingStatusResponse> GetSavedStatusAsync(ClaimsPrincipal userPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var exists = await _uow.SavedListings.Query()
            .AnyAsync(s => s.UserId == userId && s.ListingId == listingId, cancellationToken);
        return new SavedListingStatusResponse(listingId, exists);
    }

    public async Task<BasicResponse> SaveAsync(ClaimsPrincipal userPrincipal, SaveListingRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == request.ListingId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Listing not found.");
        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Only active listings can be saved.");
        }

        var existing = await _uow.SavedListings.Query()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ListingId == request.ListingId, cancellationToken);
        if (existing != null) return new BasicResponse(true, "Listing already saved.");

        var saved = new SavedListing
        {
            UserId = userId,
            ListingId = request.ListingId,
            SavedAt = DateTime.UtcNow
        };

        await _uow.SavedListings.AddAsync(saved, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Listing saved.");
    }

    public async Task<BasicResponse> RemoveAsync(ClaimsPrincipal userPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var saved = await _uow.SavedListings.Query()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ListingId == listingId, cancellationToken);
        if (saved == null) return new BasicResponse(false, "Saved listing not found.");

        _uow.SavedListings.Remove(saved);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Listing removed from saved.");
    }
}
