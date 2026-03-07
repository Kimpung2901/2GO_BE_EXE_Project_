using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPricingService
{
    AiPricingResult BuildSuggestedRange(AiPricingResult marketResult);
}
