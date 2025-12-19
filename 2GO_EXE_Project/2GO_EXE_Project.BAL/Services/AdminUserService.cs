using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using System.Text.Json;
using System.Security.Claims;

namespace _2GO_EXE_Project.BAL.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(IUnitOfWork uow, ILogger<AdminUserService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync(string? search, string? role, string? status, int skip, int take, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

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

        return users;
    }

    public async Task<AdminUserDetail?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null) return null;

        var verification = user.UserVerifications.FirstOrDefault();
        var profile = user.UserProfiles.FirstOrDefault();
        var profileInfo = profile == null
            ? null
            : new UserProfileInfo(profile.FullName, profile.DateOfBirth, profile.Gender, profile.AddressLine, profile.Bio, profile.AvatarUrl);

        return new AdminUserDetail(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.LastLoginAt,
            verification?.EmailVerified ?? false,
            verification?.PhoneVerified ?? false,
            profileInfo);
    }

    public async Task<AdminUserDetail> UpdateUserAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        user.Email = request.Email ?? user.Email;
        user.Phone = request.Phone ?? user.Phone;
        user.Status = request.Status ?? user.Status;

        var profile = user.UserProfiles.FirstOrDefault();
        var isNew = profile == null;
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _uow.UserProfiles.AddAsync(profile, cancellationToken);
        }

        profile.FullName = request.FullName ?? profile.FullName;
        profile.DateOfBirth = request.Birthday ?? profile.DateOfBirth;
        profile.Gender = request.Gender ?? profile.Gender;
        profile.AddressLine = request.Address ?? profile.AddressLine;
        profile.Bio = request.Bio ?? profile.Bio;
        profile.AvatarUrl = request.AvatarUrl ?? profile.AvatarUrl;

        if (!isNew)
        {
            _uow.UserProfiles.Update(profile);
        }

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        var verification = user.UserVerifications.FirstOrDefault();
        var profileInfo = new UserProfileInfo(profile.FullName, profile.DateOfBirth, profile.Gender, profile.AddressLine, profile.Bio, profile.AvatarUrl);

        await LogAdminActionAsync(adminPrincipal, "UpdateUser", new { TargetUserId = userId, request.Email, request.Phone, request.Status }, cancellationToken);

        return new AdminUserDetail(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.LastLoginAt,
            verification?.EmailVerified ?? false,
            verification?.PhoneVerified ?? false,
            profileInfo);
    }

    public async Task<BasicResponse> UpdateUserRoleAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Role = request.Role;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await LogAdminActionAsync(adminPrincipal, "UpdateRole", new { TargetUserId = userId, NewRole = request.Role }, cancellationToken);
        return new BasicResponse(true, "Role updated.");
    }

    public async Task<BasicResponse> UpdateUserStatusAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = request.Status;
        _uow.Users.Update(user);

        // revoke refresh tokens when disabling or deleting
        if (!string.Equals(request.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            var tokens = await _uow.RefreshTokens.Query()
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }
            _uow.RefreshTokens.UpdateRange(tokens);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await LogAdminActionAsync(adminPrincipal, "UpdateStatus", new { TargetUserId = userId, NewStatus = request.Status }, cancellationToken);
        return new BasicResponse(true, "Status updated.");
    }

    public async Task<BasicResponse> DeleteUserAsync(ClaimsPrincipal adminPrincipal, long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "User not found.");
        }

        user.Status = "Deleted";
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
        await LogAdminActionAsync(adminPrincipal, "DeleteUser", new { TargetUserId = userId, Status = "Deleted" }, cancellationToken);
        return new BasicResponse(true, "User deleted (soft).");
    }

    public async Task<IReadOnlyList<ActivityResponse>> GetAuditLogsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var logs = await _uow.ActivityLogs.Query()
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 50 : take)
            .Select(l => new ActivityResponse(l.LogId, l.Action, l.Details, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return logs;
    }

    private long? GetAdminId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    private async Task LogAdminActionAsync(ClaimsPrincipal adminPrincipal, string action, object details, CancellationToken cancellationToken)
    {
        try
        {
            var log = new ActivityLog
            {
                UserId = GetAdminId(adminPrincipal),
                Action = action,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ActivityLogs.AddAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log admin action {Action}", action);
        }
    }
}
