using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
namespace _2GO_EXE_Project.BAL.Services;
public class UserRiskInfoService : IUserRiskInfoService
{
    private readonly IUnitOfWork _uow;
    public UserRiskInfoService(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<AiUserRiskInfo> BuildUserRiskInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var id))
        {
            return new AiUserRiskInfo(0, 0, 0, 0, 0, 0, false, false);
        }
        var now = DateTime.UtcNow;
        var accountAgeDays = await _uow.Users.Query()
            .Where(u => u.UserId == id)
            .Select(u => u.CreatedAt.HasValue ? (int)(now - u.CreatedAt.Value).TotalDays : 0)
            .FirstOrDefaultAsync(cancellationToken);
        var recentListingsCount = await _uow.Listings.Query()
            .Where(l => l.SellerId == id && l.CreatedAt.HasValue && l.CreatedAt.Value >= now.AddMinutes(-10))
            .CountAsync(cancellationToken);
        var totalListingsCount = await _uow.Listings.Query()
            .Where(l => l.SellerId == id)
            .CountAsync(cancellationToken);
        var completedSalesCount = await _uow.Orders.Query()
            .Where(o => o.SellerId == id && o.Status == "Completed")
            .CountAsync(cancellationToken);
        var reportsCount = await _uow.Reports.Query()
            .Where(r => r.TargetUserId == id && r.CreatedAt.HasValue && r.CreatedAt.Value >= now.AddDays(-30))
            .CountAsync(cancellationToken);
        var deviceCount = await _uow.UserDevices.Query()
            .Where(d => d.UserId == id)
            .CountAsync(cancellationToken);
        var verification = await _uow.UserVerifications.Query()
            .Where(v => v.UserId == id)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var phoneVerified = verification?.PhoneVerified == true;
        var emailVerified = verification?.EmailVerified == true;
        return new AiUserRiskInfo(
            Math.Max(0, accountAgeDays),
            recentListingsCount,
            totalListingsCount,
            completedSalesCount,
            reportsCount,
            deviceCount,
            phoneVerified,
            emailVerified);
    }
}