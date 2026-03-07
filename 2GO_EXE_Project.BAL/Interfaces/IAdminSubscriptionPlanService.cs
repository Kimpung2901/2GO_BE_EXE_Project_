using _2GO_EXE_Project.BAL.DTOs.Subscriptions;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminSubscriptionPlanService
{
    Task<SubscriptionPlanListResponse> GetAllAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<object> GetAuditsAsync(int id, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateSubscriptionPlanRequest request, long? actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpdateSubscriptionPlanRequest request, long? actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, UpdateSubscriptionPlanStatusRequest request, long? actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdatePriceAsync(int id, UpdateSubscriptionPlanPriceRequest request, long? actorUserId, CancellationToken cancellationToken = default);
}
