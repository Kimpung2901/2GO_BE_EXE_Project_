using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class PricingService : IPricingService
{
    public AiPricingResult BuildSuggestedRange(AiPricingResult marketResult)
    {
        if (!marketResult.MarketAvg.HasValue || marketResult.MarketAvg.Value <= 0)
        {
            return marketResult;
        }

        if (string.Equals(marketResult.Confidence, "LOW", StringComparison.OrdinalIgnoreCase))
        {
            return marketResult with
            {
                SuggestedMin = null,
                SuggestedMax = null
            };
        }

        var (minRate, maxRate) = marketResult.ConditionAI switch
        {
            "NEW" => (0.80m, 0.90m),
            "GOOD" => (0.65m, 0.75m),
            "FAIR" => (0.45m, 0.60m),
            "POOR" => (0.25m, 0.40m),
            _ => (0.65m, 0.75m)
        };

        var suggestedMin = Math.Round(marketResult.MarketAvg.Value * minRate, 0);
        var suggestedMax = Math.Round(marketResult.MarketAvg.Value * maxRate, 0);

        return marketResult with
        {
            SuggestedMin = suggestedMin,
            SuggestedMax = suggestedMax
        };
    }
}
