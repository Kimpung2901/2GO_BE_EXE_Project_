using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IModerationService
{
    AiRiskResult AnalyzeRisk(
        string title,
        string description,
        decimal? listingPrice,
        decimal? suggestedMin,
        decimal? suggestedMax,
        AiUserRiskInfo userInfo);
}
