using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class SellerListingService : ISellerListingService
{
    private readonly IUnitOfWork _uow;
    public SellerListingService(IUnitOfWork uow)
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

    private async Task EnsureSubCategoryValidAsync(int subCategoryId, CancellationToken cancellationToken)
    {
        var subCategory = await _uow.SubCategories.Query()
            .FirstOrDefaultAsync(sc => sc.SubCategoryId == subCategoryId, cancellationToken);
        if (subCategory == null)
        {
            throw new InvalidOperationException("SubCategory not found.");
        }
        if (!subCategory.IsActive)
        {
            throw new InvalidOperationException("SubCategory is inactive.");
        }
    }

    private async Task EnsureWardValidAsync(int wardId, CancellationToken cancellationToken)
    {
        var ward = await _uow.Wards.Query()
            .FirstOrDefaultAsync(w => w.WardId == wardId, cancellationToken);
        if (ward == null)
        {
            throw new InvalidOperationException("Ward not found.");
        }
    }

    public async Task<SellerListingListResponse> GetMyListingsAsync(ClaimsPrincipal sellerPrincipal, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var query = _uow.Listings.Query()
            .Include(l => l.ListingImages)
            .Where(l => l.SellerId == sellerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(l => new SellerListingListItem(
                l.ListingId,
                l.Title,
                l.Price,
                l.Status,
                l.CreatedAt,
                l.UpdatedAt,
                l.ListingImages.OrderByDescending(i => i.IsPrimary == true).ThenBy(i => i.ImageId).Select(i => i.ImageUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new SellerListingListResponse(total, items);
    }

    public async Task<ListingDetail?> GetMyListingByIdAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(l => l.ListingImages)
            .Where(l => l.ListingId == listingId && l.SellerId == sellerId)
            .FirstOrDefaultAsync(cancellationToken);

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

    public async Task<ListingDetail> CreateAsync(ClaimsPrincipal sellerPrincipal, CreateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        await EnsureSubCategoryValidAsync(request.SubCategoryId, cancellationToken);
        if (request.WardId.HasValue)
        {
            await EnsureWardValidAsync(request.WardId.Value, cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }
        if (!request.Price.HasValue || request.Price.Value <= 0)
        {
            throw new InvalidOperationException("Price must be greater than 0.");
        }
        var images = request.Images?.ToList() ?? new List<ListingImageRequest>();
        if (images.Count == 0)
        {
            throw new InvalidOperationException("At least one image is required.");
        }
        var listing = new Listing
        {
            SellerId = sellerId,
            SubCategoryId = request.SubCategoryId,
            WardId = request.WardId,
            Title = request.Title,
            Description = request.Description,
            Condition = request.Condition,
            Price = request.Price,
            HasNegotiation = request.HasNegotiation,
            Dimensions = request.Dimensions,
            Weight = request.Weight,
            Brand = request.Brand,
            Status = ListingStatuses.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Listings.AddAsync(listing, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var hasPrimary = images.Any(i => i.IsPrimary);
        var listingImages = images.Select((img, index) => new ListingImage
        {
            ListingId = listing.ListingId,
            ImageUrl = img.ImageUrl,
            IsPrimary = hasPrimary ? img.IsPrimary : index == 0
        }).ToList();

        await _uow.ListingImages.AddRangeAsync(listingImages, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetMyListingByIdAsync(sellerPrincipal, listing.ListingId, cancellationToken)
               ?? throw new InvalidOperationException("Listing not found after create.");
    }

    public async Task<ListingDetail?> UpdateAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);

        if (listing == null) return null;
        if (!string.Equals(listing.Status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(listing.Status, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Listing can only be updated when status is Draft or Rejected.");
        }

        if (request.SubCategoryId.HasValue)
        {
            await EnsureSubCategoryValidAsync(request.SubCategoryId.Value, cancellationToken);
        }
        if (request.WardId.HasValue)
        {
            await EnsureWardValidAsync(request.WardId.Value, cancellationToken);
        }

        listing.Title = request.Title ?? listing.Title;
        listing.Description = request.Description ?? listing.Description;
        listing.SubCategoryId = request.SubCategoryId ?? listing.SubCategoryId;
        listing.WardId = request.WardId ?? listing.WardId;
        listing.Price = request.Price ?? listing.Price;
        listing.HasNegotiation = request.HasNegotiation ?? listing.HasNegotiation;
        listing.Condition = request.Condition ?? listing.Condition;
        listing.Brand = request.Brand ?? listing.Brand;
        listing.Dimensions = request.Dimensions ?? listing.Dimensions;
        listing.Weight = request.Weight ?? listing.Weight;
        listing.UpdatedAt = DateTime.UtcNow;

        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetMyListingByIdAsync(sellerPrincipal, listingId, cancellationToken);
    }

    public async Task<BasicResponse> PublishAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.ListingImages)
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        if (!string.Equals(listing.Status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(listing.Status, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Listing can only be submitted when status is Draft or Rejected.");
        }

        if (string.IsNullOrWhiteSpace(listing.Title))
        {
            return new BasicResponse(false, "Title is required before submitting.");
        }

        if (!listing.Price.HasValue || listing.Price.Value <= 0)
        {
            return new BasicResponse(false, "Price must be greater than 0 before submitting.");
        }

        if (!listing.SubCategoryId.HasValue)
        {
            return new BasicResponse(false, "SubCategory is required before submitting.");
        }
        await EnsureSubCategoryValidAsync(listing.SubCategoryId.Value, cancellationToken);

        var images = listing.ListingImages.ToList();
        if (images.Count == 0)
        {
            return new BasicResponse(false, "At least one image is required before submitting.");
        }

        if (!images.Any(i => i.IsPrimary == true))
        {
            images[0].IsPrimary = true;
            _uow.ListingImages.Update(images[0]);
        }

        listing.Status = ListingStatuses.PendingReview;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Listing submitted for review.");
    }

    public async Task<BasicResponse> ArchiveAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Listing can only be archived when status is Active.");
        }

        listing.Status = ListingStatuses.Archived;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Listing archived.");
    }

    public async Task<BasicResponse> UpdateImagesAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateListingImagesRequest request, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.ListingImages)
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Listing not found.");

        var existing = listing.ListingImages.ToList();
        if (existing.Count > 0)
        {
            _uow.ListingImages.RemoveRange(existing);
        }

        var images = request.Images ?? Array.Empty<ListingImageRequest>();
        if (images.Count == 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
            return new BasicResponse(true, "Images cleared.");
        }

        var hasPrimary = images.Any(i => i.IsPrimary);
        var newImages = images.Select((img, index) => new ListingImage
        {
            ListingId = listing.ListingId,
            ImageUrl = img.ImageUrl,
            IsPrimary = hasPrimary ? img.IsPrimary : index == 0
        }).ToList();

        await _uow.ListingImages.AddRangeAsync(newImages, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Images updated.");
    }

    public async Task<ListingStatsResponse?> GetMyListingStatsAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listingExists = await _uow.Listings.Query()
            .AnyAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (!listingExists) return null;

        var views = await _uow.ListingViews.Query()
            .CountAsync(v => v.ListingId == listingId, cancellationToken);
        var saves = await _uow.SavedListings.Query()
            .CountAsync(s => s.ListingId == listingId, cancellationToken);
        var inquiries = await _uow.Orders.Query()
            .CountAsync(o => o.ListingId == listingId, cancellationToken);

        return new ListingStatsResponse(listingId, views, saves, inquiries);
    }
}
