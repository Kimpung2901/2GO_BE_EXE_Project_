using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class ModeratorListingService : IModeratorListingService
{
    private readonly IUnitOfWork _uow;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly INotificationService _notificationService;
    public ModeratorListingService(IUnitOfWork uow, IMarketPriceProvider marketPriceProvider, INotificationService notificationService)
    {
        _uow = uow;
        _marketPriceProvider = marketPriceProvider;
        _notificationService = notificationService;
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
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ListingStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Trạng thái bài đăng không hợp lệ. Cho phép: {string.Join(", ", ListingStatuses.All)}.");
            }
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

    public async Task<ListingDetail?> GetByIdAsync(long listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .Include(l => l.ListingAttributes)
            .Include(l => l.Ward)
            .ThenInclude(w => w!.District)
            .Include(l => l.Seller)
            .ThenInclude(s => s!.UserProfiles)
            .FirstOrDefaultAsync(l => l.ListingId == listingId, cancellationToken);
        if (listing == null) return null;

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
            listing.Seller?.Email,
            listing.Seller?.Phone,
            primary,
            media,
            attributes,
            listing.Ward?.Name,
            listing.Ward?.District?.Name);
    }

    public async Task<BasicResponse> ApproveAsync(ClaimsPrincipal modPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng chỉ có thể được duyệt khi trạng thái là PendingReview.");
        }

        listing.Status = ListingStatuses.Active;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (listing.SellerId.HasValue)
        {
            var text = ListingNotificationText.ForStatus(ListingStatuses.Active);
            await NotifyAsync(listing.SellerId.Value, "LISTING", text.Title, text.Message, $"/listings/{listingId}", cancellationToken);
        }
        await LogModActionAsync(modPrincipal, "ApproveListing", new { ListingId = listingId }, cancellationToken);
        return new BasicResponse(true, "Bài đăng đã được duyệt.");
    }

    public async Task<BasicResponse> RejectAsync(ClaimsPrincipal modPrincipal, long listingId, RejectListingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateRejectListing(request));
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng chỉ có thể bị từ chối khi trạng thái là PendingReview.");
        }

        listing.Status = ListingStatuses.Rejected;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (listing.SellerId.HasValue)
        {
            var text = ListingNotificationText.ForStatus(ListingStatuses.Active);
            await NotifyAsync(listing.SellerId.Value, "LISTING", text.Title, text.Message, $"/listings/{listingId}", cancellationToken);
        }
        await LogModActionAsync(modPrincipal, "RejectListing", new { ListingId = listingId, request.Reason }, cancellationToken);
        return new BasicResponse(true, "Bài đăng đã bị từ chối.");
    }

    public async Task<BasicResponse> FlagAsync(ClaimsPrincipal modPrincipal, long listingId, FlagListingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateFlagListing(request));
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng chỉ có thể bị gắn cờ khi trạng thái là Active.");
        }

        listing.Status = ListingStatuses.Flagged;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (listing.SellerId.HasValue)
        {
            var text = ListingNotificationText.ForStatus(ListingStatuses.Active);
            await NotifyAsync(listing.SellerId.Value, "LISTING", text.Title, text.Message, $"/listings/{listingId}", cancellationToken);
        }
        await LogModActionAsync(modPrincipal, "FlagListing", new { ListingId = listingId, request.Reason }, cancellationToken);
        return new BasicResponse(true, "Bài đăng đã bị gắn cờ.");
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

    private async Task NotifyAsync(long userId, string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest(
                userId,
                title,
                message,
                type,
                link), cancellationToken);
        }
        catch
        {
            // ignore notification failures
        }
    }
}








