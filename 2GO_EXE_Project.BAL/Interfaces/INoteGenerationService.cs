using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface INoteGenerationService
{
    string BuildNote(AiPricingResult pricing, string conditionAi);
}
