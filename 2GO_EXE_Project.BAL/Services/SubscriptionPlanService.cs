using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Subscriptions;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly IUnitOfWork _uow;

    public SubscriptionPlanService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SubscriptionPlanListResponse> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _uow.SubscriptionPlans.Query()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .Select(x => new SubscriptionPlanItem(
                x.PlanId,
                x.Code,
                x.Name,
                x.Description,
                x.Price,
                x.DurationDays,
                x.MonthlyListingLimit,
                x.IsActive,
                x.SortOrder,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new SubscriptionPlanListResponse(items.Count, items);
    }
}
