using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
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

    public async Task<ListingListResponse> GetListingsAsync(
        string? search,
        int? categoryId,
        int? subCategoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? status,
        string? condition,
        string? brand,
        int? wardId,
        int? districtId,
        int? cityId,
        bool? hasNegotiation,
        string? sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .Include(l => l.Ward)
            .ThenInclude(w => w!.District)
            .ThenInclude(d => d!.City)
            .Include(l => l.ListingAttributes)
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
            if (!ListingStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Trạng thái bài đăng không hợp lệ. Cho phép: {string.Join(", ", ListingStatuses.All)}.");
            }
            query = query.Where(l => l.Status == status);
        }
        else
        {
            query = query.Where(l => l.Status == ListingStatuses.Active);
        }
        if (!string.IsNullOrWhiteSpace(condition))
        {
            query = query.Where(l => l.Condition != null && l.Condition.Contains(condition));
        }
        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(l => l.Brand != null && l.Brand.Contains(brand));
        }
        if (wardId.HasValue)
        {
            query = query.Where(l => l.WardId == wardId.Value);
        }
        if (districtId.HasValue)
        {
            query = query.Where(l => l.Ward != null && l.Ward.DistrictId == districtId.Value);
        }
        if (cityId.HasValue)
        {
            query = query.Where(l => l.Ward != null && l.Ward.District != null && l.Ward.District.CityId == cityId.Value);
        }
        if (hasNegotiation.HasValue)
        {
            query = query.Where(l => l.HasNegotiation == hasNegotiation.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var ordered = sort?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(l => l.Price),
            "price_desc" => query.OrderByDescending(l => l.Price),
            "oldest" => query.OrderBy(l => l.CreatedAt),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        var items = await ordered
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
                l.ListingMedias
                    .Where(m => m.MediaType == MediaTypes.Image)
                    .OrderByDescending(m => m.IsPrimary == true)
                    .ThenBy(m => m.SortOrder ?? 0)
                    .ThenBy(m => m.MediaId)
                    .Select(m => m.Url)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new ListingListResponse(total, items);
    }

    public async Task<ListingDetail?> GetListingByIdAsync(long listingId, bool onlyActive, long? viewerUserId, CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .Include(l => l.Ward)
            .ThenInclude(w => w!.District)
            .Include(l => l.Seller)
            .ThenInclude(s => s!.UserProfiles)
            .Where(l => l.ListingId == listingId);

        if (onlyActive)
        {
            query = query.Where(l => l.Status == ListingStatuses.Active);
        }

        var listing = await query.FirstOrDefaultAsync(cancellationToken);
        if (listing == null) return null;

        await TrackViewAsync(listing.ListingId, viewerUserId, cancellationToken);

        var media = listing.ListingMedias
            .OrderByDescending(m => m.IsPrimary == true)
            .ThenBy(m => m.SortOrder ?? 0)
            .ThenBy(m => m.MediaId)
            .Select(m => new ListingMediaItem(
                m.Url ?? string.Empty,
                m.MediaType ?? MediaTypes.Image,
                m.IsPrimary ?? false,
                m.SortOrder))
            .ToList();
        var primary = media
            .Where(m => m.MediaType == MediaTypes.Image)
            .Select(m => m.Url)
            .FirstOrDefault();

        var attributes = listing.ListingAttributes
            .OrderBy(a => a.AttributeId)
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new ListingAttributeItem(a.Name ?? string.Empty, a.Value ?? string.Empty))
            .ToList();

        var sellerProfile = listing.Seller?.UserProfiles
            .OrderBy(p => p.ProfileId)
            .FirstOrDefault();

        return new ListingDetail(
            listing.ListingId,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.HasNegotiation,
            listing.ListingType,
            listing.AvailableQuantity,
            listing.Condition,
            listing.Brand,
            listing.Status,
            listing.CreatedAt,
            listing.UpdatedAt,
            listing.SubCategory?.CategoryId,
            listing.SubCategoryId,
            listing.SubCategory?.Category?.Name,
            listing.SubCategory?.Name,
            listing.SellerId,
            sellerProfile?.FullName,
            sellerProfile?.AvatarUrl,
            null,
            null,
            primary,
            media,
            attributes,
            listing.Ward?.Name,
            listing.Ward?.District?.Name);
    }

    private async Task TrackViewAsync(long listingId, long? viewerUserId, CancellationToken cancellationToken)
    {
        try
        {
            var view = new _2GO_EXE_Project.DAL.Entities.ListingView
            {
                ListingId = listingId,
                UserId = viewerUserId,
                ViewedAt = DateTime.UtcNow
            };
            await _uow.ListingViews.AddAsync(view, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // ignore tracking failures
        }
    }
}

