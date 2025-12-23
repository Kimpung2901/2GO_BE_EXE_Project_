using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Reports;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }
        return id;
    }

    public async Task<ReportResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }
        if (!request.ListingId.HasValue && !request.TargetUserId.HasValue)
        {
            throw new InvalidOperationException("ListingId or TargetUserId is required.");
        }

        var report = new Report
        {
            ReporterId = userId,
            ListingId = request.ListingId,
            TargetUserId = request.TargetUserId,
            Reason = request.Reason,
            Status = ReportStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Reports.AddAsync(report, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ReportResponse(report.ReportId, report.ListingId, report.TargetUserId, report.Reason, report.Status, report.CreatedAt);
    }

    public async Task<ReportListResponse> GetMyReportsAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var query = _uow.Reports.Query()
            .Where(r => r.ReporterId == userId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(r => new ReportResponse(r.ReportId, r.ListingId, r.TargetUserId, r.Reason, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReportListResponse(total, items);
    }
}
