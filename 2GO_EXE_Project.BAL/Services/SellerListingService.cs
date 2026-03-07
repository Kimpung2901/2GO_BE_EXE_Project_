using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class SellerListingService : ISellerListingService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;
    private readonly IAiListingService _aiListingService;
    private readonly string _cloudinaryCloudName;
    private static readonly TimeZoneInfo VnTimeZone = ResolveVnTimeZone();

    public SellerListingService(
        IUnitOfWork uow,
        INotificationService notificationService,
        IOptions<CloudinarySettings> cloudinaryOptions,
        IAiListingService aiListingService)
    {
        _uow = uow;
        _notificationService = notificationService;
        _cloudinaryCloudName = cloudinaryOptions.Value.CloudName ?? string.Empty;
        _aiListingService = aiListingService;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    private async Task EnsureSubCategoryValidAsync(int subCategoryId, CancellationToken cancellationToken)
    {
        var subCategory = await _uow.SubCategories.Query()
            .FirstOrDefaultAsync(sc => sc.SubCategoryId == subCategoryId, cancellationToken);
        if (subCategory == null)
        {
            throw new InvalidOperationException("Không tìm thấy danh mục con.");
        }
        if (!subCategory.IsActive)
        {
            throw new InvalidOperationException("Danh mục con đang bị vô hiệu hóa.");
        }
    }

    private async Task EnsureWardValidAsync(int wardId, CancellationToken cancellationToken)
    {
        var ward = await _uow.Wards.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WardId == wardId, cancellationToken);
        if (ward == null)
        {
            var result = new ValidationResult();
            result.Add("wardId", "WardId không tồn tại.");
            throw new CustomValidationException(result.Errors, "INVALID_LOCATION");
        }
        if (!ward.DistrictId.HasValue || !ward.CityId.HasValue)
        {
            var result = new ValidationResult();
            result.Add("wardId", "WardId thiếu thông tin quận/huyện hoặc thành phố.");
            throw new CustomValidationException(result.Errors, "INVALID_LOCATION");
        }
    }

    private static List<ListingAttribute> BuildListingAttributes(long listingId, IEnumerable<ListingAttributeRequest> requests)
    {
        return requests
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .Select(r => new ListingAttribute
            {
                ListingId = listingId,
                Name = r.Name.Trim(),
                Value = string.IsNullOrWhiteSpace(r.Value) ? null : r.Value.Trim()
            })
            .ToList();
    }

    private static string NormalizeMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return MediaTypes.Image;
        }

        var normalized = mediaType.Trim().ToUpperInvariant();
        if (!MediaTypes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Loại media không hợp lệ. Cho phép: {string.Join(", ", MediaTypes.All)}.");
        }

        return normalized;
    }

    private static void ValidateMediaRequests(IReadOnlyList<ListingMediaRequest> mediaRequests)
    {
        ValidationGuard.ThrowIfInvalid(ListingValidator.ValidateMedia(mediaRequests));
    }

    private void ValidateCloudinaryMediaUrls(IReadOnlyList<ListingMediaRequest> mediaRequests)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(_cloudinaryCloudName))
        {
            result.Add("media", "Cloudinary CloudName chưa được cấu hình.");
            ValidationGuard.ThrowIfInvalid(result);
        }

        var cloudPathPrefix = $"/{_cloudinaryCloudName.Trim()}/";
        for (var i = 0; i < mediaRequests.Count; i++)
        {
            var url = mediaRequests[i].Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                result.Add($"media[{i}].url", "Media url phải là URL Cloudinary tuyệt đối hợp lệ.");
                continue;
            }

            if (!string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"media[{i}].url", "Media url phải được lưu trữ trên Cloudinary.");
                continue;
            }

            if (!uri.AbsolutePath.StartsWith(cloudPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"media[{i}].url", "Media url phải thuộc Cloudinary cloud đã cấu hình.");
            }
        }

        ValidationGuard.ThrowIfInvalid(result);
    }

    public async Task<SellerListingListResponse> GetMyListingsAsync(ClaimsPrincipal sellerPrincipal, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var query = _uow.Listings.Query()
            .Include(l => l.ListingMedias)
            .Where(l => l.SellerId == sellerId);

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
            .Select(l => new SellerListingListItem(
                l.ListingId,
                l.Title,
                l.Price,
                l.Status,
                l.CreatedAt,
                l.UpdatedAt,
                l.ListingMedias
                    .Where(m => m.MediaType == MediaTypes.Image)
                    .OrderByDescending(m => m.IsPrimary == true)
                    .ThenBy(m => m.SortOrder ?? 0)
                    .ThenBy(m => m.MediaId)
                    .Select(m => m.Url)
                    .FirstOrDefault(),
                _uow.Orders.Query()
                    .Where(o => o.ListingId == l.ListingId &&
                                (o.Status == OrderStatuses.Pending ||
                                 o.Status == OrderStatuses.Confirmed ||
                                 o.Status == OrderStatuses.Delivering ||
                                 o.Status == OrderStatuses.Delivered ||
                                 o.Status == OrderStatuses.Completed ||
                                 o.Status == OrderStatuses.Disputed))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.PaymentMethod)
                    .FirstOrDefault(),
                _uow.Orders.Query()
                    .Where(o => o.ListingId == l.ListingId &&
                                (o.Status == OrderStatuses.Pending ||
                                 o.Status == OrderStatuses.Confirmed ||
                                 o.Status == OrderStatuses.Delivering ||
                                 o.Status == OrderStatuses.Delivered ||
                                 o.Status == OrderStatuses.Completed ||
                                 o.Status == OrderStatuses.Disputed))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (bool?)_uow.Payments.Query()
                        .Any(p => p.OrderId == o.OrderId &&
                                  (p.PaymentStage == PaymentStages.Deposit || p.PaymentStage == null) &&
                                  p.Status == PaymentStatuses.Paid))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new SellerListingListResponse(total, items);
    }

    public async Task<ListingDetail?> GetMyListingByIdAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.SubCategory)
            .ThenInclude(sc => sc!.Category)
            .Include(l => l.ListingMedias)
            .Include(l => l.ListingAttributes)
            .Include(l => l.Ward)
            .ThenInclude(w => w!.District)
            .Include(l => l.Seller)
            .ThenInclude(s => s!.UserProfiles)
            .Where(l => l.ListingId == listingId && l.SellerId == sellerId)
            .FirstOrDefaultAsync(cancellationToken);

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

    public async Task<ListingDetail> CreateAsync(ClaimsPrincipal sellerPrincipal, CreateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(ListingValidator.ValidateCreate(request));
        var sellerId = GetUserId(sellerPrincipal);
        await EnsureSubCategoryValidAsync(request.SubCategoryId, cancellationToken);
        if (request.WardId.HasValue)
        {
            await EnsureWardValidAsync(request.WardId.Value, cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Tiêu đề là bắt buộc.");
        }
        var status = request.Status;
        if (string.IsNullOrWhiteSpace(status))
        {
            status = ListingStatuses.Draft;
        }

        if (!string.Equals(status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bài đăng chỉ có thể tạo ở trạng thái Draft. Hãy dùng endpoint publish để gửi duyệt.");
        }

        var mediaRequests = request.Media?.ToList() ?? new List<ListingMediaRequest>();
        var imageRequests = mediaRequests
            .Where(m => string.IsNullOrWhiteSpace(m.MediaType) ||
                        m.MediaType == MediaTypes.Image)
            .ToList();

        if (string.Equals(status, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            if (imageRequests.Count == 0)
            {
                throw new InvalidOperationException("Cần ít nhất một ảnh.");
            }
        }
        ValidateMediaRequests(mediaRequests);
        ValidateCloudinaryMediaUrls(mediaRequests);
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
            ListingType = ListingTypes.Single,
            AvailableQuantity = 1,
            Dimensions = request.Dimensions,
            Weight = request.Weight,
            Brand = request.Brand,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Listings.AddAsync(listing, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var hasPrimaryImage = imageRequests.Any(i => i.IsPrimary);
        var listingMedia = new List<ListingMedia>();
        var imageIndex = 0;
        for (var index = 0; index < mediaRequests.Count; index++)
        {
            var item = mediaRequests[index];
            var normalizedType = NormalizeMediaType(item.MediaType);
            var isImage = string.Equals(normalizedType, MediaTypes.Image, StringComparison.OrdinalIgnoreCase);
            var isPrimary = isImage && (hasPrimaryImage ? item.IsPrimary : imageIndex == 0);
            if (isImage)
            {
                imageIndex++;
            }

            listingMedia.Add(new ListingMedia
            {
                ListingId = listing.ListingId,
                Url = item.Url,
                MediaType = normalizedType,
                IsPrimary = isPrimary,
                SortOrder = item.SortOrder ?? index
            });
        }

        await _uow.ListingMedias.AddRangeAsync(listingMedia, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var attributeRequests = request.Attributes ?? Array.Empty<ListingAttributeRequest>();
        var listingAttributes = BuildListingAttributes(listing.ListingId, attributeRequests);
        if (listingAttributes.Count > 0)
        {
            await _uow.ListingAttributes.AddRangeAsync(listingAttributes, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return await GetMyListingByIdAsync(sellerPrincipal, listing.ListingId, cancellationToken)
               ?? throw new InvalidOperationException("Không tìm thấy bài đăng sau khi tạo.");
    }

    public async Task<ListingDetail?> UpdateAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(ListingValidator.ValidateUpdate(request));
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);

        if (listing == null) return null;
        if (!string.Equals(listing.Status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(listing.Status, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bài đăng chỉ có thể cập nhật khi trạng thái là Draft hoặc Rejected.");
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
        // Force SINGLE for thanh-ly model
        if (!string.IsNullOrWhiteSpace(request.ListingType) &&
            !string.Equals(request.ListingType, ListingTypes.Single, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ListingType chỉ hỗ trợ SINGLE cho sàn này.");
        }
        if (request.AvailableQuantity.HasValue && request.AvailableQuantity.Value != 1)
        {
            throw new InvalidOperationException("AvailableQuantity phải là 1 đối với listing SINGLE.");
        }
        listing.ListingType = ListingTypes.Single;
        listing.AvailableQuantity = 1;
        listing.Condition = request.Condition ?? listing.Condition;
        listing.Brand = request.Brand ?? listing.Brand;
        listing.Dimensions = request.Dimensions ?? listing.Dimensions;
        listing.Weight = request.Weight ?? listing.Weight;
        listing.UpdatedAt = DateTime.UtcNow;

        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);

        if (request.Attributes != null)
        {
            var existing = await _uow.ListingAttributes.Query()
                .Where(a => a.ListingId == listingId)
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                _uow.ListingAttributes.RemoveRange(existing);
            }

            var newAttributes = BuildListingAttributes(listingId, request.Attributes);
            if (newAttributes.Count > 0)
            {
                await _uow.ListingAttributes.AddRangeAsync(newAttributes, cancellationToken);
            }
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return await GetMyListingByIdAsync(sellerPrincipal, listingId, cancellationToken);
    }

    public async Task<BasicResponse> PublishAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.ListingMedias)
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(listing.Status, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng chỉ có thể gửi duyệt khi trạng thái là Draft hoặc Rejected.");
        }

        if (string.IsNullOrWhiteSpace(listing.Title))
        {
            return new BasicResponse(false, "Cần có tiêu đề trước khi gửi duyệt.");
        }

        if (!listing.Price.HasValue || listing.Price.Value < 0)
        {
            return new BasicResponse(false, "Giá phải lớn hơn hoặc bằng 0 trước khi gửi duyệt.");
        }

        if (!listing.SubCategoryId.HasValue)
        {
            return new BasicResponse(false, "Cần chọn danh mục con trước khi gửi duyệt.");
        }
        await EnsureSubCategoryValidAsync(listing.SubCategoryId.Value, cancellationToken);

        var media = listing.ListingMedias.ToList();
        var images = media.Where(m => m.MediaType == MediaTypes.Image).ToList();
        if (images.Count == 0)
        {
            return new BasicResponse(false, "Cần ít nhất một ảnh trước khi gửi duyệt.");
        }

        if (!images.Any(i => i.IsPrimary == true))
        {
            images[0].IsPrimary = true;
            _uow.ListingMedias.Update(images[0]);
        }

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == sellerId, cancellationToken);
        if (user == null) return new BasicResponse(false, "Không tìm thấy người dùng.");
        var eligibilityError = GetSellerEligibilityError(user);
        if (!string.IsNullOrWhiteSpace(eligibilityError))
        {
            return new BasicResponse(false, eligibilityError);
        }

        var categoryId = await _uow.SubCategories.Query()
            .Where(sc => sc.SubCategoryId == listing.SubCategoryId.Value)
            .Select(sc => sc.CategoryId ?? 0)
            .FirstOrDefaultAsync(cancellationToken);
        if (categoryId <= 0)
        {
            return new BasicResponse(false, "Không tìm thấy danh mục cho bài đăng.");
        }

        var precheckRequest = new AiListingPrecheckRequest(
            listing.Title ?? string.Empty,
            listing.Description ?? string.Empty,
            categoryId,
            listing.Brand,
            listing.Price,
            images.Select(i => i.Url ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            sellerId.ToString());

        var precheck = await _aiListingService.PrecheckAsync(precheckRequest, deepChecks: true, cancellationToken);
        var qualityDecision = precheck.Quality.Decision?.Trim();
        var isQualityReject = string.Equals(qualityDecision, "REJECT", StringComparison.OrdinalIgnoreCase);
        var isQualityManual = string.Equals(qualityDecision, "MANUAL_REVIEW", StringComparison.OrdinalIgnoreCase);
        if (isQualityReject)
        {
            return new BasicResponse(false, "Ảnh không đạt kiểm tra chất lượng. Vui lòng cập nhật ảnh.");
        }

        if (string.Equals(precheck.Risk.Action, "REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng bị từ chối do kiểm tra rủi ro. Vui lòng chỉnh sửa nội dung.");
        }

        var now = DateTime.UtcNow;
        var plan = await ResolveCurrentPlanAsync(user.UserId, now, cancellationToken);
        if (plan?.MonthlyListingLimit is int limit)
        {
            var vnNow = TimeZoneInfo.ConvertTimeFromUtc(now, VnTimeZone);
            var monthStartVn = new DateTime(vnNow.Year, vnNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var nextMonthStartVn = monthStartVn.AddMonths(1);
            var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartVn, VnTimeZone);
            var nextMonthStartUtc = TimeZoneInfo.ConvertTimeToUtc(nextMonthStartVn, VnTimeZone);
            var publishedCount = await _uow.Listings.Query()
                .Where(l => l.SellerId == sellerId &&
                            l.PublishedAt.HasValue &&
                            l.PublishedAt.Value >= monthStartUtc &&
                            l.PublishedAt.Value < nextMonthStartUtc)
                .CountAsync(cancellationToken);
            if (publishedCount >= limit)
            {
                return new BasicResponse(false, "Bạn đã đạt giới hạn đăng bài của gói hiện tại. Vui lòng nâng cấp để tiếp tục.");
            }
        }

        var targetStatus =
            isQualityManual || string.Equals(precheck.Risk.Action, "PENDING_REVIEW", StringComparison.OrdinalIgnoreCase)
                ? ListingStatuses.PendingReview
                : ListingStatuses.Active;

        listing.Status = targetStatus;
        listing.PublishedAt = now;
        listing.UpdatedAt = now;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);
        var notifyText = ListingNotificationText.ForStatus(targetStatus);
        await NotifyAsync(sellerId, "LISTING", notifyText.Title, notifyText.Message, $"/seller/listings/{listing.ListingId}", cancellationToken);
        if (string.Equals(targetStatus, ListingStatuses.PendingReview, StringComparison.OrdinalIgnoreCase))
        {
            await NotifyAdminsAsync("LISTING_REVIEW", "Có bài đăng cần duyệt", $"Bài đăng #{listing.ListingId} đang chờ duyệt.", $"/admin/listings/{listing.ListingId}", cancellationToken);
        }
        return new BasicResponse(true, targetStatus == ListingStatuses.Active ? "Bài đăng đã được đăng." : "Bài đăng đã được gửi duyệt.");
    }

    public async Task<BasicResponse> ArchiveAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Bài đăng chỉ có thể lưu trữ khi trạng thái là Active.");
        }

        listing.Status = ListingStatuses.Archived;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);
        await NotifyAsync(sellerId, "LISTING", ListingNotificationText.ForStatus(ListingStatuses.Archived).Title, ListingNotificationText.ForStatus(ListingStatuses.Archived).Message, $"/seller/listings/{listing.ListingId}", cancellationToken);
        return new BasicResponse(true, "Bài đăng đã được lưu trữ.");
    }

    public async Task<BasicResponse> DeleteAsync(ClaimsPrincipal sellerPrincipal, long listingId, CancellationToken cancellationToken = default)
    {
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (string.Equals(listing.Status, ListingStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Bài đăng đã bị xóa.");
        }

        listing.Status = ListingStatuses.Deleted;
        listing.UpdatedAt = DateTime.UtcNow;
        _uow.Listings.Update(listing);
        await _uow.SaveChangesAsync(cancellationToken);
        await NotifyAsync(sellerId, "LISTING", ListingNotificationText.ForStatus(ListingStatuses.Deleted).Title, ListingNotificationText.ForStatus(ListingStatuses.Deleted).Message, $"/seller/listings/{listing.ListingId}", cancellationToken);
        return new BasicResponse(true, "Bài đăng đã bị xóa (mềm).");
    }

    public async Task<BasicResponse> UpdateMediaAsync(ClaimsPrincipal sellerPrincipal, long listingId, UpdateListingMediaRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(ListingValidator.ValidateMedia(request.Media));
        var sellerId = GetUserId(sellerPrincipal);
        var listing = await _uow.Listings.Query()
            .Include(l => l.ListingMedias)
            .FirstOrDefaultAsync(l => l.ListingId == listingId && l.SellerId == sellerId, cancellationToken);
        if (listing == null) return new BasicResponse(false, "Không tìm thấy bài đăng.");

        if (!string.Equals(listing.Status, ListingStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(listing.Status, ListingStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Media chỉ có thể cập nhật khi trạng thái là Draft hoặc Rejected.");
        }

        var existing = listing.ListingMedias.ToList();
        if (existing.Count > 0)
        {
            _uow.ListingMedias.RemoveRange(existing);
        }

        var mediaRequests = request.Media ?? Array.Empty<ListingMediaRequest>();
        if (mediaRequests.Count == 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
            return new BasicResponse(true, "Đã xóa media.");
        }
        ValidateMediaRequests(mediaRequests);
        ValidateCloudinaryMediaUrls(mediaRequests);

        var imageRequests = mediaRequests
            .Where(m => string.IsNullOrWhiteSpace(m.MediaType) ||
                        m.MediaType == MediaTypes.Image)
            .ToList();

        var hasPrimaryImage = imageRequests.Any(i => i.IsPrimary);
        var newMedia = new List<ListingMedia>();
        var imageIndex = 0;
        for (var index = 0; index < mediaRequests.Count; index++)
        {
            var item = mediaRequests[index];
            var normalizedType = NormalizeMediaType(item.MediaType);
            var isImage = string.Equals(normalizedType, MediaTypes.Image, StringComparison.OrdinalIgnoreCase);
            var isPrimary = isImage && (hasPrimaryImage ? item.IsPrimary : imageIndex == 0);
            if (isImage)
            {
                imageIndex++;
            }

            newMedia.Add(new ListingMedia
            {
                ListingId = listing.ListingId,
                Url = item.Url,
                MediaType = normalizedType,
                IsPrimary = isPrimary,
                SortOrder = item.SortOrder ?? index
            });
        }

        await _uow.ListingMedias.AddRangeAsync(newMedia, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Đã cập nhật media.");
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

    private static string NormalizeListingType(string? listingType)
    {
        if (string.IsNullOrWhiteSpace(listingType)) return ListingTypes.Single;
        if (string.Equals(listingType, ListingTypes.Single, StringComparison.OrdinalIgnoreCase)) return ListingTypes.Single;
        throw new InvalidOperationException("ListingType chỉ hỗ trợ SINGLE cho sàn này.");
    }

    private static string? GetSellerEligibilityError(User user)
    {
        var profile = user.UserProfiles
            .OrderBy(p => p.ProfileId)
            .FirstOrDefault();

        var verification = user.UserVerifications
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefault();

        var missing = new List<string>();
        if (profile == null) missing.Add("profile");
        if (profile != null && string.IsNullOrWhiteSpace(profile.FullName)) missing.Add("fullName");
        if (string.IsNullOrWhiteSpace(user.Phone)) missing.Add("phone");
        if (profile != null && string.IsNullOrWhiteSpace(profile.AddressLine)) missing.Add("address");
        if (profile != null && string.IsNullOrWhiteSpace(profile.BankAccountNumber)) missing.Add("bankAccountNumber");
        if (profile != null && string.IsNullOrWhiteSpace(profile.BankBin)) missing.Add("bankBin");
        if (profile != null && string.IsNullOrWhiteSpace(profile.BankAccountName)) missing.Add("bankAccountName");

        var phoneVerified = verification?.PhoneVerified == true;
        var emailVerified = verification?.EmailVerified == true;
        if (!phoneVerified && !emailVerified) missing.Add("verification");

        if (missing.Count == 0) return null;

        return "Vui lòng hoàn thiện hồ sơ (fullName, phone, address, bankAccountNumber, bankBin, bankAccountName) và xác minh tài khoản (email hoặc phone) trước khi gửi bài đăng. " +
               $"Thiếu: {string.Join(", ", missing.Distinct(StringComparer.OrdinalIgnoreCase))}.";
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

    private async Task NotifyAdminsAsync(string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            var adminIds = await _uow.Users.Query()
                .Where(u => u.Role == UserRoles.Admin || u.Role == UserRoles.Manager)
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            foreach (var id in adminIds)
            {
                await _notificationService.CreateAsync(new CreateNotificationRequest(
                    id,
                    title,
                    message,
                    type,
                    link), cancellationToken);
            }
        }
        catch
        {
            // ignore notification failures
        }
    }

    private async Task<SubscriptionPlan?> ResolveCurrentPlanAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        var userSub = await _uow.Users.Query()
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.SubscriptionPlanCode, u.SubscriptionValidUntil, u.SubscriptionUntil })
            .FirstOrDefaultAsync(cancellationToken);

        if (userSub != null)
        {
            var validUntil = userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil;
            if (validUntil.HasValue &&
                validUntil.Value > now &&
                !string.IsNullOrWhiteSpace(userSub.SubscriptionPlanCode))
            {
                var code = userSub.SubscriptionPlanCode.Trim().ToUpperInvariant();
                var planByCode = await _uow.SubscriptionPlans.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
                if (planByCode != null) return planByCode;
            }
        }

        var hasActiveSubscription = userSub != null &&
                                    (userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil).HasValue &&
                                    (userSub.SubscriptionValidUntil ?? userSub.SubscriptionUntil)!.Value > now;

        if (hasActiveSubscription)
        {
            var payment = await _uow.Payments.Query()
                .Where(p => p.UserId == userId &&
                            p.PaymentType == PaymentTypes.Subscription &&
                            p.Status == PaymentStatuses.Paid &&
                            p.SubscriptionValidUntil.HasValue &&
                            p.SubscriptionValidUntil.Value > now)
                .OrderByDescending(p => p.SubscriptionValidUntil)
                .FirstOrDefaultAsync(cancellationToken);

            var plan = await ResolvePlanForPaymentAsync(payment, cancellationToken);
            if (plan != null) return plan;
            return null;
        }

        var freePlan = await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Price <= 0, cancellationToken);
        return freePlan;
    }

    private async Task<SubscriptionPlan?> ResolvePlanForPaymentAsync(Payment? payment, CancellationToken cancellationToken)
    {
        if (payment == null) return null;

        if (!string.IsNullOrWhiteSpace(payment.SubscriptionPlanCode))
        {
            var code = payment.SubscriptionPlanCode.Trim().ToUpperInvariant();
            return await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
        }

        if (payment.Amount.HasValue && payment.SubscriptionDays.HasValue)
        {
            return await _uow.SubscriptionPlans.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.DurationDays == payment.SubscriptionDays.Value &&
                    p.Price == payment.Amount.Value, cancellationToken);
        }

        return null;
    }

    private static TimeZoneInfo ResolveVnTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}













