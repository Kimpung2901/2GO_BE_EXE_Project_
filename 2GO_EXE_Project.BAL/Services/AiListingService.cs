using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class AiListingService : IAiListingService
{
    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private readonly IAiQualityCheckService _qualityCheckService;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly IPricingService _pricingService;
    private readonly IUserPrecheckService _precheckService;
    private readonly IUserRiskInfoService _userRiskInfoService;
    private readonly INoteGenerationService _noteGenerationService;
    private readonly INotificationService _notificationService;
    private readonly IGeminiService _geminiService;
    private readonly IUnitOfWork _uow;

    public AiListingService(
        IAiQualityCheckService qualityCheckService,
        IMarketPriceProvider marketPriceProvider,
        IPricingService pricingService,
        IUserPrecheckService precheckService,
        IUserRiskInfoService userRiskInfoService,
        INoteGenerationService noteGenerationService,
        INotificationService notificationService,
        IGeminiService geminiService,
        IUnitOfWork uow)
    {
        _qualityCheckService = qualityCheckService;
        _marketPriceProvider = marketPriceProvider;
        _pricingService = pricingService;
        _precheckService = precheckService;
        _userRiskInfoService = userRiskInfoService;
        _noteGenerationService = noteGenerationService;
        _notificationService = notificationService;
        _geminiService = geminiService;
        _uow = uow;
    }

    public async Task<AiListingAnalyzeResponse> AnalyzeAsync(AiListingAnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        var quality = await _qualityCheckService.CheckAsync(request.MediaUrls, deepChecks: true, cancellationToken);

        var conditionAi = await InferConditionAsync(request.Title, request.Description, request.MediaUrls, request.UserId, cancellationToken);
        var productKey = await BuildProductKeyAsync(request.CategoryId, request.Brand, request.Title, cancellationToken);
        var market = await _marketPriceProvider.GetMarketPriceAsync(
            new MarketPriceInput(productKey, request.CategoryId, conditionAi),
            cancellationToken);

        var pricing = new AiPricingResult(
            productKey,
            market.MarketAvg,
            market.Confidence,
            conditionAi,
            null,
            null);
        pricing = _pricingService.BuildSuggestedRange(pricing);

        var userInfo = await _userRiskInfoService.BuildUserRiskInfoAsync(request.UserId, cancellationToken);
        var risk = _precheckService.Evaluate(
            request.Title,
            request.Description,
            request.Price,
            pricing.SuggestedMin,
            pricing.SuggestedMax,
            userInfo);

        var note = _noteGenerationService.BuildNote(pricing, conditionAi);

        var recommendation = ResolveRecommendation(quality, risk);

        var response = new AiListingAnalyzeResponse(quality, pricing, risk, note, recommendation);
        await LogAnalysisAsync(request, response, cancellationToken);
        await QueueManualReviewIfNeededAsync(request, response, cancellationToken);
        return response;
    }

    public async Task<AiListingPrecheckResponse> PrecheckAsync(AiListingPrecheckRequest request, bool deepChecks, CancellationToken cancellationToken = default)
    {
        var quality = await _qualityCheckService.CheckAsync(request.MediaUrls, deepChecks, cancellationToken);

        var conditionAi = await InferConditionAsync(request.Title, request.Description, request.MediaUrls, request.UserId, cancellationToken);
        var productKey = await BuildProductKeyAsync(request.CategoryId, request.Brand, request.Title, cancellationToken);
        var market = await _marketPriceProvider.GetMarketPriceAsync(
            new MarketPriceInput(productKey, request.CategoryId, conditionAi),
            cancellationToken);

        var pricing = new AiPricingResult(
            productKey,
            market.MarketAvg,
            market.Confidence,
            conditionAi,
            null,
            null);
        pricing = _pricingService.BuildSuggestedRange(pricing);

        var userInfo = await _userRiskInfoService.BuildUserRiskInfoAsync(request.UserId, cancellationToken);
        var risk = _precheckService.Evaluate(
            request.Title,
            request.Description,
            request.Price,
            pricing.SuggestedMin,
            pricing.SuggestedMax,
            userInfo);

        var note = _noteGenerationService.BuildNote(pricing, conditionAi);
        var canPublish = quality.Decision == "PASS" && risk.Action != "REJECTED";

        return new AiListingPrecheckResponse(quality, pricing, risk, note, canPublish);
    }

    private async Task<string> BuildProductKeyAsync(int categoryId, string? brand, string title, CancellationToken cancellationToken)
    {
        var categoryName = await _uow.Categories.Query()
            .Where(x => x.CategoryId == categoryId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(brand)) parts.Add(brand);
        if (!string.IsNullOrWhiteSpace(categoryName)) parts.Add(categoryName);
        if (parts.Count == 0) parts.Add(title);
        return NormalizeKey(string.Join(" ", parts));
    }

    private static string NormalizeKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return SpaceRegex.Replace(input, " ").Trim().ToLowerInvariant();
    }
    private async Task<string> InferConditionAsync(string title, string description, IReadOnlyList<string> mediaUrls, string userId, CancellationToken cancellationToken)
    {
        var vision = await TryGeminiConditionAsync(title, description, mediaUrls, userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(vision))
        {
            return vision!;
        }
        var text = $"{title} {description}".ToLowerInvariant();
        if (text.Contains("mới") || text.Contains("like new") || text.Contains("99%"))
        {
            return "NEW";
        }
        if (text.Contains("xước") || text.Contains("trầy") || text.Contains("cũ"))
        {
            return "FAIR";
        }
        if (text.Contains("hỏng") || text.Contains("lỗi"))
        {
            return "POOR";
        }
        return "GOOD";
    }

    private async Task<string?> TryGeminiConditionAsync(string title, string description, IReadOnlyList<string> mediaUrls, string userId, CancellationToken cancellationToken)
    {
        if (mediaUrls == null || mediaUrls.Count == 0) return null;
        try
        {
            var cached = await _uow.AiImageVisionCaches.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ImageUrl == mediaUrls[0], cancellationToken);
            if (!string.IsNullOrWhiteSpace(cached?.ConditionLabel))
            {
                return cached.ConditionLabel;
            }
            var prompt =
                "Hãy phân loại tình trạng sản phẩm theo 1 trong 4 nhãn: NEW, GOOD, FAIR, POOR. " +
                "Chỉ trả về đúng 1 nhãn.\n" +
                $"Tiêu đề: {title}\nMô tả: {description}";
            var text = await _geminiService.GenerateFromImageAsync(prompt, mediaUrls[0], userId, cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) return null;
            var normalized = text.Trim().ToUpperInvariant();
            string? label = null;
            if (normalized.Contains("NEW")) label = "NEW";
            else if (normalized.Contains("GOOD")) label = "GOOD";
            else if (normalized.Contains("FAIR")) label = "FAIR";
            else if (normalized.Contains("POOR")) label = "POOR";

            if (!string.IsNullOrWhiteSpace(label))
            {
                var existing = await _uow.AiImageVisionCaches.Query().FirstOrDefaultAsync(x => x.ImageUrl == mediaUrls[0], cancellationToken);
                if (existing == null)
                {
                    await _uow.AiImageVisionCaches.AddAsync(new DAL.Entities.AiImageVisionCache
                    {
                        ImageUrl = mediaUrls[0],
                        ConditionLabel = label,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
                else
                {
                    existing.ConditionLabel = label;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _uow.AiImageVisionCaches.Update(existing);
                }
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return label;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveRecommendation(AiQualityResult quality, AiRiskResult risk)
    {
        if (quality.Decision == "REJECT")
        {
            return AiListingRecommendations.RejectedDraft;
        }

        if (quality.Decision == "MANUAL_REVIEW" || risk.Action is "PENDING_REVIEW" or "REJECTED")
        {
            return AiListingRecommendations.PendingReview;
        }

        return AiListingRecommendations.Published;
    }

    private async Task LogAnalysisAsync(AiListingAnalyzeRequest request, AiListingAnalyzeResponse response, CancellationToken cancellationToken)
    {
        var log = new AiAnalysisLog
        {
            Type = "ListingAnalyze",
            UserId = request.UserId,
            RequestJson = JsonSerializer.Serialize(request),
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AiAnalysisLogs.AddAsync(log, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueManualReviewIfNeededAsync(AiListingAnalyzeRequest request, AiListingAnalyzeResponse response, CancellationToken cancellationToken)
    {
        if (response.FinalRecommendation != AiListingRecommendations.PendingReview &&
            response.Risk.Action is not "PENDING_REVIEW" and not "REJECTED" &&
            response.Quality.Decision != "MANUAL_REVIEW")
        {
            return;
        }

        var reasonParts = new List<string>();
        if (response.Quality.Decision != "PASS") reasonParts.Add($"Quality:{response.Quality.Decision}");
        if (response.Risk.Flags.Count > 0) reasonParts.Add(string.Join(",", response.Risk.Flags));

        var queue = new ManualReviewQueue
        {
            ListingId = request.ListingId,
            Reason = string.Join(" | ", reasonParts),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ManualReviewQueues.AddAsync(queue, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await NotifyAdminsAsync(
            "RISK",
            "Bài đăng cần kiểm tra",
            $"Listing #{request.ListingId} cần kiểm tra thủ công.",
            $"/admin/listings/{request.ListingId}",
            cancellationToken);
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
}











