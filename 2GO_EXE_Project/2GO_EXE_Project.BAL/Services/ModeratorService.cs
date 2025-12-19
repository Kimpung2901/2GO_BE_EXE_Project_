using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ModeratorService : IModeratorService
{
    private readonly IUnitOfWork _uow;

    public ModeratorService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private long? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    public async Task<AdminUserListResponse> GetUsersAsync(string? search, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Users.Query()
            .Include(u => u.UserVerifications)
            .Include(u => u.UserProfiles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)) ||
                u.UserProfiles.Any(p => p.FullName != null && p.FullName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(u => new AdminUserSummary(
                u.UserId,
                u.Email,
                u.Phone,
                u.Role,
                u.Status,
                u.CreatedAt,
                u.LastLoginAt,
                u.UserVerifications.FirstOrDefault().EmailVerified ?? false,
                u.UserVerifications.FirstOrDefault().PhoneVerified ?? false,
                u.UserProfiles.FirstOrDefault().FullName))
            .ToListAsync(cancellationToken);

        return new AdminUserListResponse(total, users);
    }

    public async Task<BasicResponse> BanUserAsync(ClaimsPrincipal modPrincipal, long userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = "Banned";
        if (request.DurationDays.HasValue)
        {
            user.BanUntil = DateTime.UtcNow.AddDays(request.DurationDays.Value);
        }
        _uow.Users.Update(user);

        var tokens = await _uow.RefreshTokens.Query()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }
        _uow.RefreshTokens.UpdateRange(tokens);

        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "BanUser", new { TargetUserId = userId, request.Reason, request.DurationDays }, cancellationToken);
        return new BasicResponse(true, "User banned.");
    }

    public async Task<BasicResponse> UnbanUserAsync(ClaimsPrincipal modPrincipal, long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = "Active";
        user.BanUntil = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "UnbanUser", new { TargetUserId = userId }, cancellationToken);
        return new BasicResponse(true, "User unbanned.");
    }

    public async Task<ReportListResponse> GetReportsAsync(string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Reports.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(r => new ReportSummary(r.ReportId, r.ReporterId, r.TargetUserId, r.ListingId, r.Reason, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReportListResponse(total, items);
    }

    public async Task<ReportDetail?> GetReportByIdAsync(long reportId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Reports.Query()
            .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);
        if (report == null) return null;
        return new ReportDetail(report.ReportId, report.ReporterId, report.TargetUserId, report.ListingId, report.Reason, report.Status, report.CreatedAt);
    }

    public async Task<BasicResponse> ResolveReportAsync(ClaimsPrincipal modPrincipal, long reportId, ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null)
        {
            return new BasicResponse(false, "Report not found.");
        }

        report.Status = string.IsNullOrWhiteSpace(request.Status) ? "Resolved" : request.Status;
        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(cancellationToken);

        await LogModActionAsync(modPrincipal, "ResolveReport", new { ReportId = reportId, report.Status, request.Note }, cancellationToken);
        return new BasicResponse(true, "Report updated.");
    }

    private async Task LogModActionAsync(ClaimsPrincipal principal, string action, object details, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ActivityLogs.AddAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // swallow logging errors
        }
    }
}
