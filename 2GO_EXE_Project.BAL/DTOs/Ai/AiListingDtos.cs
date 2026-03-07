namespace _2GO_EXE_Project.BAL.DTOs.Ai;

public record AiListingAnalyzeRequest(
    string Title,
    string Description,
    int CategoryId,
    string? Brand,
    int? Year,
    decimal? Price,
    IReadOnlyList<string> MediaUrls,
    string UserId,
    long ListingId);

public record AiQualityResult(
    double Score,
    IReadOnlyList<string> Issues,
    string Decision);

public record AiPricingResult(
    string DetectedProduct,
    decimal? MarketAvg,
    string Confidence,
    string ConditionAI,
    decimal? SuggestedMin,
    decimal? SuggestedMax);

public record AiUserRiskInfo(
    int AccountAgeDays,
    int RecentListingsCount,
    int TotalListingsCount,
    int CompletedSalesCount,
    int ReportsCount,
    int DeviceCount,
    bool PhoneVerified,
    bool EmailVerified);

public record AiRiskResult(
    double RiskScore,
    IReadOnlyList<string> Flags,
    string Action,
    string RiskConfidence = "HIGH");

public record AiListingAnalyzeResponse(
    AiQualityResult Quality,
    AiPricingResult Pricing,
    AiRiskResult Risk,
    string Note,
    string FinalRecommendation);
