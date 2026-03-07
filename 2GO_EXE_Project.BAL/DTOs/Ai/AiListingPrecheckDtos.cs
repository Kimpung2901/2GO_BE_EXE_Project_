using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.DTOs.Ai;

public record AiListingPrecheckRequest(
    string Title,
    string Description,
    int CategoryId,
    string? Brand,
    decimal? Price,
    IReadOnlyList<string> MediaUrls,
    string UserId);

public record AiListingPrecheckResponse(
    AiQualityResult Quality,
    AiPricingResult Pricing,
    AiRiskResult Risk,
    string Note,
    bool CanPublish);