namespace _2GO_EXE_Project.BAL.Settings;

public class SubscriptionPlanSettings
{
    public string FreePlanCode { get; set; } = "BASIC";
    public string DefaultPaidPlanCode { get; set; } = "PREMIUM";
    public List<SubscriptionPlanDefinition> Plans { get; set; } = new();
}

public class SubscriptionPlanDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int? MonthlyListingLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
