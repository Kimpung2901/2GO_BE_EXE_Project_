using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAiListingService
{
    Task<AiListingAnalyzeResponse> AnalyzeAsync(AiListingAnalyzeRequest request, CancellationToken cancellationToken = default);
    Task<AiListingPrecheckResponse> PrecheckAsync(AiListingPrecheckRequest request, bool deepChecks, CancellationToken cancellationToken = default);
}
