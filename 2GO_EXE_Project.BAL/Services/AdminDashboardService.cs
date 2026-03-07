using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Admin;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _uow;

    public AdminDashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AdminDashboardResponse> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var toDate = to ?? now;
        var fromDate = from ?? now.AddDays(-30);
        if (fromDate > toDate)
        {
            throw new InvalidOperationException("Khoảng thời gian không hợp lệ.");
        }

        var ordersQuery = _uow.Orders.Query().AsNoTracking()
            .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value >= fromDate && o.CreatedAt.Value <= toDate);

        var paymentsQuery = _uow.Payments.Query().AsNoTracking()
            .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= fromDate && p.CreatedAt.Value <= toDate);

        var listingsQuery = _uow.Listings.Query().AsNoTracking()
            .Where(l => l.CreatedAt.HasValue && l.CreatedAt.Value >= fromDate && l.CreatedAt.Value <= toDate);

        var usersQuery = _uow.Users.Query().AsNoTracking()
            .Where(u => u.CreatedAt.HasValue && u.CreatedAt.Value >= fromDate && u.CreatedAt.Value <= toDate);

        var reportsQuery = _uow.Reports.Query().AsNoTracking()
            .Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value >= fromDate && r.CreatedAt.Value <= toDate);

        var escrowQuery = _uow.EscrowContracts.Query().AsNoTracking()
            .Where(e => e.CreatedAt.HasValue && e.CreatedAt.Value >= fromDate && e.CreatedAt.Value <= toDate);

        var ordersTotal = await ordersQuery.CountAsync(cancellationToken);
        var ordersCompleted = await ordersQuery.CountAsync(o => o.Status == OrderStatuses.Completed, cancellationToken);
        var ordersCancelled = await ordersQuery.CountAsync(o => o.Status == OrderStatuses.Cancelled, cancellationToken);
        var gmvCompleted = await ordersQuery
            .Where(o => o.Status == OrderStatuses.Completed)
            .SumAsync(o => o.TotalAmount ?? 0m, cancellationToken);

        var paymentsPaid = await paymentsQuery.CountAsync(p => p.Status == PaymentStatuses.Paid, cancellationToken);
        var paymentsFailed = await paymentsQuery.CountAsync(p => p.Status == PaymentStatuses.Failed || p.Status == PaymentStatuses.Cancelled, cancellationToken);
        var subscriptionRevenue = await paymentsQuery
            .Where(p => p.PaymentType == PaymentTypes.Subscription && p.Status == PaymentStatuses.Paid)
            .SumAsync(p => p.Amount ?? 0m, cancellationToken);
        var commissionRevenue = await paymentsQuery
            .Where(p => p.PaymentType == PaymentTypes.Commission && p.Status == PaymentStatuses.Paid)
            .SumAsync(p => (p.CommissionBaseAmount ?? 0m) * (p.CommissionRate ?? 0m), cancellationToken);
        var escrowHeldAmount = await escrowQuery
            .Where(e => e.Status == EscrowStatuses.Funded || e.Status == EscrowStatuses.Holding)
            .SumAsync(e => e.DepositAmount ?? 0m, cancellationToken);
        var escrowReleasedAmount = await escrowQuery
            .Where(e => e.Status == EscrowStatuses.Released)
            .SumAsync(e => e.DepositAmount ?? 0m, cancellationToken);

        var ordersCancelledRate = ordersTotal == 0 ? 0m : (decimal)ordersCancelled / ordersTotal;
        var paymentsTotal = paymentsPaid + paymentsFailed;
        var paymentsFailedRate = paymentsTotal == 0 ? 0m : (decimal)paymentsFailed / paymentsTotal;

        var usersByPlan = await BuildUsersByPlanAsync(cancellationToken);

        var listingsNew = await listingsQuery.CountAsync(cancellationToken);
        var listingsActive = await _uow.Listings.Query().AsNoTracking().CountAsync(l => l.Status == ListingStatuses.Active, cancellationToken);
        var listingsPending = await _uow.Listings.Query().AsNoTracking().CountAsync(l => l.Status == ListingStatuses.PendingReview, cancellationToken);

        var usersNew = await usersQuery.CountAsync(cancellationToken);
        var reportsNew = await reportsQuery.CountAsync(cancellationToken);

        var summary = new AdminKpiSummary(
            gmvCompleted,
            ordersTotal,
            ordersCompleted,
            ordersCancelled,
            listingsNew,
            listingsActive,
            listingsPending,
            usersNew,
            paymentsPaid,
            paymentsFailed,
            subscriptionRevenue,
            commissionRevenue,
            escrowHeldAmount,
            escrowReleasedAmount,
            ordersCancelledRate,
            paymentsFailedRate,
            usersByPlan,
            reportsNew);

        return new AdminDashboardResponse(fromDate, toDate, summary);
    }

    public async Task<AdminTimeseriesResponse> GetTimeseriesAsync(DateTime? from, DateTime? to, string bucket, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var toDate = to ?? now;
        var fromDate = from ?? now.AddDays(-30);
        if (fromDate > toDate)
        {
            throw new InvalidOperationException("Khoảng thời gian không hợp lệ.");
        }

        var bucketKey = NormalizeBucket(bucket);
        if (bucketKey == null)
        {
            throw new InvalidOperationException("Bucket không hợp lệ. Dùng day|week|month|year.");
        }

        var points = BuildBuckets(fromDate, toDate, bucketKey.Value);
        if (points.Count == 0)
        {
            return new AdminTimeseriesResponse(fromDate, toDate, bucketKey.Value.ToString().ToLowerInvariant(), new List<AdminTimeseriesPoint>());
        }

        var orderDaily = await _uow.Orders.Query().AsNoTracking()
            .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value >= fromDate && o.CreatedAt.Value <= toDate)
            .GroupBy(o => o.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                OrdersTotal = g.Count(),
                OrdersCompleted = g.Count(o => o.Status == OrderStatuses.Completed),
                OrdersCancelled = g.Count(o => o.Status == OrderStatuses.Cancelled),
                GmvCompleted = g.Where(o => o.Status == OrderStatuses.Completed).Sum(o => o.TotalAmount ?? 0m)
            })
            .ToListAsync(cancellationToken);

        var paymentDaily = await _uow.Payments.Query().AsNoTracking()
            .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= fromDate && p.CreatedAt.Value <= toDate)
            .GroupBy(p => p.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                PaymentsPaid = g.Count(p => p.Status == PaymentStatuses.Paid),
                PaymentsFailed = g.Count(p => p.Status == PaymentStatuses.Failed || p.Status == PaymentStatuses.Cancelled),
                SubscriptionRevenue = g.Where(p => p.PaymentType == PaymentTypes.Subscription && p.Status == PaymentStatuses.Paid)
                    .Sum(p => p.Amount ?? 0m)
                ,
                CommissionRevenue = g.Where(p => p.PaymentType == PaymentTypes.Commission && p.Status == PaymentStatuses.Paid)
                    .Sum(p => (p.CommissionBaseAmount ?? 0m) * (p.CommissionRate ?? 0m))
            })
            .ToListAsync(cancellationToken);

        var listingDaily = await _uow.Listings.Query().AsNoTracking()
            .Where(l => l.CreatedAt.HasValue && l.CreatedAt.Value >= fromDate && l.CreatedAt.Value <= toDate)
            .GroupBy(l => l.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                ListingsNew = g.Count()
            })
            .ToListAsync(cancellationToken);

        var userDaily = await _uow.Users.Query().AsNoTracking()
            .Where(u => u.CreatedAt.HasValue && u.CreatedAt.Value >= fromDate && u.CreatedAt.Value <= toDate)
            .GroupBy(u => u.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                UsersNew = g.Count()
            })
            .ToListAsync(cancellationToken);

        var reportDaily = await _uow.Reports.Query().AsNoTracking()
            .Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value >= fromDate && r.CreatedAt.Value <= toDate)
            .GroupBy(r => r.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                ReportsNew = g.Count()
            })
            .ToListAsync(cancellationToken);

        var escrowDaily = await _uow.EscrowContracts.Query().AsNoTracking()
            .Where(e => e.CreatedAt.HasValue && e.CreatedAt.Value >= fromDate && e.CreatedAt.Value <= toDate)
            .GroupBy(e => e.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Day = g.Key,
                EscrowHeldAmount = g.Where(e => e.Status == EscrowStatuses.Funded || e.Status == EscrowStatuses.Holding)
                    .Sum(e => e.DepositAmount ?? 0m),
                EscrowReleasedAmount = g.Where(e => e.Status == EscrowStatuses.Released)
                    .Sum(e => e.DepositAmount ?? 0m)
            })
            .ToListAsync(cancellationToken);

        var orderByPeriod = orderDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                OrdersTotal = g.Sum(x => x.OrdersTotal),
                OrdersCompleted = g.Sum(x => x.OrdersCompleted),
                OrdersCancelled = g.Sum(x => x.OrdersCancelled),
                GmvCompleted = g.Sum(x => x.GmvCompleted)
            });

        var paymentByPeriod = paymentDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                PaymentsPaid = g.Sum(x => x.PaymentsPaid),
                PaymentsFailed = g.Sum(x => x.PaymentsFailed),
                SubscriptionRevenue = g.Sum(x => x.SubscriptionRevenue),
                CommissionRevenue = g.Sum(x => x.CommissionRevenue)
            });

        var listingByPeriod = listingDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                ListingsNew = g.Sum(x => x.ListingsNew)
            });

        var userByPeriod = userDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                UsersNew = g.Sum(x => x.UsersNew)
            });

        var reportByPeriod = reportDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                ReportsNew = g.Sum(x => x.ReportsNew)
            });

        var escrowByPeriod = escrowDaily
            .GroupBy(x => GetBucketStart(x.Day, bucketKey.Value))
            .ToDictionary(g => g.Key, g => new
            {
                EscrowHeldAmount = g.Sum(x => x.EscrowHeldAmount),
                EscrowReleasedAmount = g.Sum(x => x.EscrowReleasedAmount)
            });

        var resultPoints = new List<AdminTimeseriesPoint>(points.Count);
        foreach (var periodStart in points)
        {
            orderByPeriod.TryGetValue(periodStart, out var og);
            paymentByPeriod.TryGetValue(periodStart, out var pg);
            listingByPeriod.TryGetValue(periodStart, out var lg);
            userByPeriod.TryGetValue(periodStart, out var ug);
            reportByPeriod.TryGetValue(periodStart, out var rg);
            escrowByPeriod.TryGetValue(periodStart, out var eg);

            resultPoints.Add(new AdminTimeseriesPoint(
                periodStart,
                og?.GmvCompleted ?? 0m,
                og?.OrdersTotal ?? 0,
                og?.OrdersCompleted ?? 0,
                og?.OrdersCancelled ?? 0,
                lg?.ListingsNew ?? 0,
                ug?.UsersNew ?? 0,
                pg?.PaymentsPaid ?? 0,
                pg?.PaymentsFailed ?? 0,
                pg?.SubscriptionRevenue ?? 0m,
                pg?.CommissionRevenue ?? 0m,
                eg?.EscrowHeldAmount ?? 0m,
                eg?.EscrowReleasedAmount ?? 0m,
                rg?.ReportsNew ?? 0));
        }

        return new AdminTimeseriesResponse(fromDate, toDate, bucketKey.Value.ToString().ToLowerInvariant(), resultPoints);
    }

    private enum Bucket
    {
        Day,
        Week,
        Month,
        Year
    }

    private static Bucket? NormalizeBucket(string bucket)
    {
        if (string.IsNullOrWhiteSpace(bucket)) return Bucket.Day;
        var key = bucket.Trim().ToLowerInvariant();
        return key switch
        {
            "day" => Bucket.Day,
            "week" => Bucket.Week,
            "month" => Bucket.Month,
            "year" => Bucket.Year,
            _ => null
        };
    }

    private static List<DateTime> BuildBuckets(DateTime fromDate, DateTime toDate, Bucket bucket)
    {
        var points = new List<DateTime>();
        var cursor = AlignStart(fromDate, bucket);
        var end = toDate;
        while (cursor <= end)
        {
            points.Add(cursor);
            cursor = AddBucket(cursor, bucket);
        }
        return points;
    }

    private static DateTime AlignStart(DateTime date, Bucket bucket)
    {
        var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return bucket switch
        {
            Bucket.Day => new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc),
            Bucket.Week => StartOfWeek(utc),
            Bucket.Month => new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            Bucket.Year => new DateTime(utc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => utc
        };
    }

    private static DateTime StartOfWeek(DateTime dateUtc)
    {
        var day = (int)dateUtc.DayOfWeek;
        var diff = (day == 0 ? 6 : day - 1); // Monday as start
        var start = dateUtc.AddDays(-diff);
        return new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime AddBucket(DateTime date, Bucket bucket)
    {
        return bucket switch
        {
            Bucket.Day => date.AddDays(1),
            Bucket.Week => date.AddDays(7),
            Bucket.Month => date.AddMonths(1),
            Bucket.Year => date.AddYears(1),
            _ => date.AddDays(1)
        };
    }

    private static DateTime GetBucketStart(DateTime date, Bucket bucket)
    {
        var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return bucket switch
        {
            Bucket.Day => new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc),
            Bucket.Week => StartOfWeek(utc),
            Bucket.Month => new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            Bucket.Year => new DateTime(utc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => utc
        };
    }

    private async Task<IReadOnlyList<AdminPlanCount>> BuildUsersByPlanAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var totalUsers = await _uow.Users.Query().CountAsync(cancellationToken);

        var activeSubs = await _uow.Payments.Query().AsNoTracking()
            .Where(p => p.PaymentType == PaymentTypes.Subscription &&
                        p.Status == PaymentStatuses.Paid &&
                        p.UserId.HasValue &&
                        p.SubscriptionValidUntil.HasValue &&
                        p.SubscriptionValidUntil.Value > now)
            .Select(p => new { p.UserId, p.SubscriptionPlanCode })
            .ToListAsync(cancellationToken);

        var activeUserIds = activeSubs.Select(x => x.UserId!.Value).Distinct().ToHashSet();
        var activeByPlan = activeSubs
            .Where(x => !string.IsNullOrWhiteSpace(x.SubscriptionPlanCode))
            .GroupBy(x => x.SubscriptionPlanCode!.Trim().ToUpperInvariant())
            .Select(g => new { Code = g.Key, Users = g.Select(x => x.UserId!.Value).Distinct().Count() })
            .ToList();

        var plans = await _uow.SubscriptionPlans.Query().AsNoTracking()
            .Select(p => new { p.Code, p.Name, p.Price })
            .ToListAsync(cancellationToken);

        var planNameByCode = plans.ToDictionary(p => p.Code.Trim().ToUpperInvariant(), p => p.Name);

        var results = new List<AdminPlanCount>();
        foreach (var plan in activeByPlan)
        {
            var name = planNameByCode.TryGetValue(plan.Code, out var n) ? n : plan.Code;
            results.Add(new AdminPlanCount(plan.Code, name, plan.Users));
        }

        var basicCode = plans.FirstOrDefault(p => p.Price <= 0)?.Code ?? "BASIC";
        var basicName = plans.FirstOrDefault(p => p.Code == basicCode)?.Name ?? "Basic";
        var basicUsers = Math.Max(0, totalUsers - activeUserIds.Count);
        results.Add(new AdminPlanCount(basicCode, basicName, basicUsers));

        return results
            .OrderBy(r => r.Code)
            .ToList();
    }
}

