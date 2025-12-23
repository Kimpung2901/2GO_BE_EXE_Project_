using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ModeratorListingService : IModeratorListingService
{
    private readonly IUnitOfWork _uow;
    public ModeratorListingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    public async Task<ListingListResponse> GetListingsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(l => l.ListingImages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(l => new ListingListItem(
                l.ListingId,
                l.Title,
                l.Price,
                l.Status,
                l.CreatedAt,
                l.SubCategory != null ? l.SubCategory.CategoryId : null,
                l.SubCategoryId,
                l.SubCategory != null && l.SubCategory.Category != null ? l.SubCategory.Category.Name : null,
                l.SubCategory != null ? l.SubCategory.Name : null,
                l.ListingImages.OrderByDescending(i => i.IsPrimary == true).ThenBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new ListingListResponse(total, items);
    }

    public async Task<ListingDetail?> GetByIdAsync(long listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(l => l.ListingImages)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.ListingId == listingId, cancellationToken);
        if (listing == null) return null;

        var images = listing.ListingImages
            .OrderByDescending(i => i.IsPrimary == true)
            .ThenBy(i => i.ImageId)
            .Select(i => i.ImageUrl ?? string.Empty)
            .ToList();
        var primary = images.FirstOrDefault();

        return new ListingDetail(
            listing.ListingId,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.HasNegotiation,
            listing.Condition,
            listing.Brand,
            listing.Status,
            listing.CreatedAt,
            listing.UpdatedAt,
            listing.SubCategory?.CategoryId,
            listing.SubCategoryId,
            listing.SubCategory?.Category?.Name,
            listing.SubCategory?.Name,
            listing.Seller?.Email,
            listing.Seller?.Phone,
            primary,
            images);
    }

    public async Task<BasicResponse> ApproveAsync(ClaimsPrincipal modPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        if (!string.Equals(listing.Status, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Listing can only be approved when status is PendingReview.");
        }

        listing.Status = ListingStatuses.Active;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "ApproveListing", new { ListingId = listingId }, cancellationToken);
        return new BasicResponse(true, "Listing approved.");
    }

    public async Task<BasicResponse> RejectAsync(ClaimsPrincipal modPrincipal, long listingId, RejectListingRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        if (!string.Equals(listing.Status, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Listing can only be rejected when status is PendingReview.");
        }

        listing.Status = ListingStatuses.Rejected;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "RejectListing", new { ListingId = listingId, request.Reason }, cancellationToken);
        return new BasicResponse(true, "Listing rejected.");
    }

    public async Task<BasicResponse> FlagAsync(ClaimsPrincipal modPrincipal, long listingId, FlagListingRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Listing can only be flagged when status is Active.");
        }

        listing.Status = ListingStatuses.Flagged;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "FlagListing", new { ListingId = listingId, request.Reason }, cancellationToken);
        return new BasicResponse(true, "Listing flagged.");
    }

    private async Task LogModActionAsync(ClaimsPrincipal principal, string action, object details, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ActivityLogs.AddAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // ignore logging failures
        }
    }
}
