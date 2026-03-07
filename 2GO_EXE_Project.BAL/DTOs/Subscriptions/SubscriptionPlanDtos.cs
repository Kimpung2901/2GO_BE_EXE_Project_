namespace _2GO_EXE_Project.BAL.DTOs.Subscriptions;

public record SubscriptionPlanItem(
    int PlanId,
    string Code,
    string Name,
    string? Description,
    decimal Price,
    int DurationDays,
    int? MonthlyListingLimit,
    bool IsActive,
    int SortOrder,
    DateTime? UpdatedAt);

public record SubscriptionPlanListResponse(int Total, IReadOnlyList<SubscriptionPlanItem> Items);

public record CreateSubscriptionPlanRequest(
    string Code,
    string Name,
    string? Description,
    decimal Price,
    int DurationDays,
    int? MonthlyListingLimit,
    bool IsActive,
    int SortOrder);

public record UpdateSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    int DurationDays,
    int? MonthlyListingLimit,
    bool IsActive,
    int SortOrder);

public record UpdateSubscriptionPlanStatusRequest(bool IsActive);

public record UpdateSubscriptionPlanPriceRequest(decimal Price);

public record UserSubscriptionResponse(
    long UserId,
    string? SubscriptionPlanCode,
    string? SubscriptionPlanName,
    DateTime? SubscriptionUntil,
    bool IsActive,
    int? RemainingDays);

public record UserSubscriptionUsageResponse(
    int? MonthlyListingLimit,
    int UsedCount,
    int? RemainingCount);
