using _2GO_EXE_Project.BAL.DTOs.Subscriptions;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanListResponse> GetActiveAsync(CancellationToken cancellationToken = default);
}
