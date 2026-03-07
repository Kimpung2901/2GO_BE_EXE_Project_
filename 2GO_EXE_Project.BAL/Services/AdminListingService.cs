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

public class AdminListingService : IAdminListingService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private static readonly HashSet<string> AllowedStatuses = new(ListingStatuses.All, StringComparer.OrdinalIgnoreCase);

    public AdminListingService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
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

    public async Task<ListingListResponse> GetListingsAsync(string? status, long? sellerId, int? categoryId, int? subCategoryId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!AllowedStatuses.Contains(status))
            {
                throw new InvalidOperationException($"Trạng thái bài đăng không hợp lệ. Cho phép: {string.Join(", ", ListingStatuses.All)}.");
            }
            query = query.Where(l => l.Status == status);
        }
        if (sellerId.HasValue)
        {
            query = query.Where(l => l.SellerId == sellerId.Value);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(l => l.SubCategory != null && l.SubCategory.CategoryId == categoryId.Value);
        }
        if (subCategoryId.HasValue)
        {
            query = query.Where(l => l.SubCategoryId == subCategoryId.Value);
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
        var query = _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .Include(l => l.ListingAttributes)
            .Include(l => l.Ward)
            .ThenInclude(w => w!.District)
            .Include(l => l.Seller)
            .ThenInclude(s => s!.UserProfiles)
            .Where(l => l.ListingId == listingId);

        var listing = await query.FirstOrDefaultAsync(cancellationToken);
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

    public async Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal adminPrincipal, long listingId, UpdateListingStatusRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateUpdateListingStatus(request));
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!AllowedStatuses.Contains(request.Status))
        {
            return new BasicResponse(false, "Giá trị trạng thái không hợp lệ.");
        }

        if (string.Equals(listing.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Bài đăng đã ở trạng thái yêu cầu.");
        }

        if (!IsStatusTransitionAllowed(listing.Status, request.Status))
        {
            return new BasicResponse(false, $"Chuyển trạng thái bài đăng không hợp lệ: {listing.Status} -> {request.Status}.");
        }

        listing.Status = request.Status;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (listing.SellerId.HasValue)
        {
            await NotifyAsync(listing.SellerId.Value, "LISTING", ListingNotificationText.ForStatus(request.Status).Title, ListingNotificationText.ForStatus(request.Status).Message, $"/listings/{listingId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "UpdateListingStatus", new { ListingId = listingId, request.Status }, cancellationToken);
        return new BasicResponse(true, "Đã cập nhật trạng thái bài đăng.");
    }

    private static bool IsStatusTransitionAllowed(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current)) return true;

        if (string.Equals(current, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Archived, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Flagged, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Sold, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Archived, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Flagged, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Reserved, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Sold, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Sold, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }

    public async Task<BasicResponse> DeleteAsync(ClaimsPrincipal adminPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        listing.Status = ListingStatuses.Deleted;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (listing.SellerId.HasValue)
        {
            await NotifyAsync(listing.SellerId.Value, "LISTING", ListingNotificationText.ForStatus(ListingStatuses.Deleted).Title, ListingNotificationText.ForStatus(ListingStatuses.Deleted).Message, $"/listings/{listingId}", cancellationToken);
        }
        await LogAdminActionAsync(adminPrincipal, "DeleteListing", new { ListingId = listingId }, cancellationToken);
        return new BasicResponse(true, "Bài đăng đã bị xóa (mềm).");
    }

    private async Task LogAdminActionAsync(ClaimsPrincipal principal, string action, object details, CancellationToken cancellationToken)
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






