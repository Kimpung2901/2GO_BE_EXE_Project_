using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using System.Text.Json;
using System.Security.Claims;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdminUserService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public AdminUserService(IUnitOfWork uow, IPasswordHasher passwordHasher, ILogger<AdminUserService> logger)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AdminUserListResponse> GetUsersAsync(string? search, string? role, string? status, int skip, int take, CancellationToken cancellationToken = default)
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
            if (!UserStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Trạng thái người dùng không hợp lệ. Cho phép: {string.Join(", ", UserStatuses.All)}.");
            }
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
                u.UserVerifications.Select(v => v.EmailVerified).FirstOrDefault() ?? false,
                u.UserVerifications.Select(v => v.PhoneVerified).FirstOrDefault() ?? false,
                u.UserProfiles.Select(p => p.FullName).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new AdminUserListResponse(total, users);
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
            : new UserProfileInfo(
                profile.FullName,
                profile.DateOfBirth,
                profile.Gender,
                profile.AddressLine,
                profile.Bio,
                profile.AvatarUrl,
                profile.BankAccountNumber,
                profile.BankBin,
                profile.BankAccountName);

        return new AdminUserDetail(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.BanUntil,
            user.CreatedAt,
            user.LastLoginAt,
            verification?.EmailVerified ?? false,
            verification?.PhoneVerified ?? false,
            profileInfo);
    }

    public async Task<AdminUserDetail> CreateUserAsync(ClaimsPrincipal adminPrincipal, AdminCreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateAdminCreate(request));

        var status = string.IsNullOrWhiteSpace(request.Status) ? UserStatuses.Active : request.Status;

        var normalizedRole = UserRoles.Normalize(request.Role);
        var exists = await _uow.Users.Query()
            .AnyAsync(u => (!string.IsNullOrEmpty(request.Email) && u.Email == request.Email) || (!string.IsNullOrEmpty(request.Phone) && u.Phone == request.Phone), cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Người dùng đã tồn tại.");
        }

        string? passwordHash = null;
        string? salt = null;
        if (!string.IsNullOrEmpty(request.Password))
        {
            passwordHash = _passwordHasher.HashPassword(request.Password, out salt);
        }

        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = passwordHash,
            Salt = salt,
            Role = normalizedRole,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // profile
        var profile = new UserProfile
        {
            UserId = user.UserId,
            FullName = request.FullName,
            DateOfBirth = request.Birthday,
            Gender = NormalizeGender(request.Gender),
            AddressLine = request.Address,
            Bio = request.Bio,
            AvatarUrl = request.AvatarUrl
        };
        await _uow.UserProfiles.AddAsync(profile, cancellationToken);

        // verification default
        var verification = new UserVerification
        {
            UserId = user.UserId,
            EmailVerified = false,
            PhoneVerified = false,
            VerifiedAt = null
        };
        await _uow.UserVerifications.AddAsync(verification, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        await LogAdminActionAsync(adminPrincipal, "CreateUser", new { TargetUserId = user.UserId, request.Email, request.Phone, Role = normalizedRole, request.Status }, cancellationToken);

        var profileInfo = new UserProfileInfo(
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.AddressLine,
            profile.Bio,
            profile.AvatarUrl,
            profile.BankAccountNumber,
            profile.BankBin,
            profile.BankAccountName);

        return new AdminUserDetail(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.BanUntil,
            user.CreatedAt,
            user.LastLoginAt,
            verification.EmailVerified ?? false,
            verification.PhoneVerified ?? false,
            profileInfo);
    }

    public async Task<AdminUserDetail> UpdateUserAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(UserValidator.ValidateAdminUpdate(request));

        var user = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .Include(u => u.UserVerifications)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("Không tìm thấy người dùng.");
        }

        user.Email = request.Email ?? user.Email;
        user.Phone = request.Phone ?? user.Phone;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            user.Status = request.Status;
        }

        var profile = user.UserProfiles.FirstOrDefault();
        var isNew = profile == null;
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _uow.UserProfiles.AddAsync(profile, cancellationToken);
        }

        profile.FullName = request.FullName ?? profile.FullName;
        profile.DateOfBirth = request.Birthday ?? profile.DateOfBirth;
        profile.Gender = NormalizeGender(request.Gender) ?? profile.Gender;
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
        var profileInfo = new UserProfileInfo(
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.AddressLine,
            profile.Bio,
            profile.AvatarUrl,
            profile.BankAccountNumber,
            profile.BankBin,
            profile.BankAccountName);

        await LogAdminActionAsync(adminPrincipal, "UpdateUser", new { TargetUserId = userId, request.Email, request.Phone, request.Status }, cancellationToken);

        return new AdminUserDetail(
            user.UserId,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.BanUntil,
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
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        if (!UserRoles.All.Contains(request.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Vai trò không hợp lệ. Cho phép: User, Manager, Admin.");
        }
        var normalizedRole = UserRoles.Normalize(request.Role);
        user.Role = normalizedRole;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await LogAdminActionAsync(adminPrincipal, "UpdateRole", new { TargetUserId = userId, NewRole = normalizedRole }, cancellationToken);
        return new BasicResponse(true, "Đã cập nhật vai trò.");
    }

    public async Task<BasicResponse> UpdateUserStatusAsync(ClaimsPrincipal adminPrincipal, long userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        if (string.IsNullOrWhiteSpace(request.Status) || !UserStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, "Trạng thái không hợp lệ. Cho phép: Active, Banned, Deleted.");
        }

        user.Status = request.Status;
        _uow.Users.Update(user);

        // revoke refresh tokens when disabling or deleting
        if (!string.Equals(request.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
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
        return new BasicResponse(true, "Đã cập nhật trạng thái.");
    }

    public async Task<BasicResponse> DeleteUserAsync(ClaimsPrincipal adminPrincipal, long userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new BasicResponse(false, "Không tìm thấy người dùng.");
        }

        user.Status = UserStatuses.Deleted;
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
        await LogAdminActionAsync(adminPrincipal, "DeleteUser", new { TargetUserId = userId, Status = UserStatuses.Deleted }, cancellationToken);
        return new BasicResponse(true, "Người dùng đã bị xóa (mềm).");
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

    private static string? NormalizeGender(string? gender)
    {
        if (gender == null) return null;
        var trimmed = gender.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}





