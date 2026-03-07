using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class UserPrecheckService : IUserPrecheckService
{
    private readonly IModerationService _moderationService;

    public UserPrecheckService(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    public AiRiskResult Evaluate(
        string title,
        string description,
        decimal? listingPrice,
        decimal? suggestedMin,
        decimal? suggestedMax,
        AiUserRiskInfo userInfo)
    {
        var adminRisk = _moderationService.AnalyzeRisk(
            title,
            description,
            listingPrice,
            suggestedMin,
            suggestedMax,
            userInfo);

        if (string.Equals(adminRisk.Action, "REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            var flags = adminRisk.Flags.ToList();
            flags.Add("REQUIRES_ADMIN_REVIEW");
            return new AiRiskResult(adminRisk.RiskScore, flags, "PENDING_REVIEW");
        }

        return adminRisk;
    }
}
