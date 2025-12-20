using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ListingService : IListingService
{
    private readonly IUnitOfWork _uow;

    public ListingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ListingListResponse> GetListingsAsync(string? search, int? categoryId, int? subCategoryId, decimal? minPrice, decimal? maxPrice, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(l => l.ListingImages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.Title != null && l.Title.Contains(search));
        }
        if (categoryId.HasValue)
        {
            query = query.Where(l => l.SubCategory != null && l.SubCategory.CategoryId == categoryId.Value);
        }
        if (subCategoryId.HasValue)
        {
            query = query.Where(l => l.SubCategoryId == subCategoryId.Value);
        }
        if (minPrice.HasValue)
        {
            query = query.Where(l => l.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= maxPrice.Value);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }
        else
        {
            query = query.Where(l => l.Status == "Active");
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
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
                l.ListingImages.OrderByDescending(img => img.IsPrimary == true).ThenBy(img => img.ImageId).Select(img => img.ImageUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new ListingListResponse(total, items);
    }

    public async Task<ListingDetail?> GetListingByIdAsync(long listingId, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(l => l.ListingImages)
            .Include(l => l.Seller)
            .Where(l => l.ListingId == listingId);

        if (onlyActive)
        {
            query = query.Where(l => l.Status == "Active");
        }

        var listing = await query.FirstOrDefaultAsync(cancellationToken);
        if (listing == null) return null;

        var images = listing.ListingImages
            .OrderByDescending(img => img.IsPrimary == true)
            .ThenBy(img => img.ImageId)
            .Select(img => img.ImageUrl ?? string.Empty)
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
}
