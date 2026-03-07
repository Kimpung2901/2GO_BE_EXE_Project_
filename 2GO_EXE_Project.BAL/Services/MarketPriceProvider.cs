using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class MarketPriceProvider : IMarketPriceProvider
{
    private const int SampleThreshold = 10;
    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MarketPriceProvider> _logger;

    public MarketPriceProvider(IUnitOfWork uow, ILogger<MarketPriceProvider> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<MarketPriceResult> GetMarketPriceAsync(MarketPriceInput input, CancellationToken cancellationToken = default)
    {
        var productKey = NormalizeKey(input.ProductKey);
        if (string.IsNullOrWhiteSpace(productKey))
        {
            return Fail("empty_product_key", 0);
        }

        var condition = NormalizeCondition(input.Condition);
        var query = _uow.MarketPrices.Query();
        query = query.Where(x => x.ProductKey == productKey && x.Condition == condition);
        if (input.CategoryId.HasValue)
        {
            var categoryId = input.CategoryId.Value;
            query = query.Where(x => x.CategoryId == categoryId);
        }

        var record = await query.FirstOrDefaultAsync(cancellationToken);
        if (record != null && record.SampleCount >= SampleThreshold && record.AvgPrice > 0)
        {
            var confidence = record.SampleCount >= 20 ? "HIGH" : "MEDIUM";
            return new MarketPriceResult(record.AvgPrice, record.MinPrice, record.MaxPrice, record.SampleCount, "internal_market", confidence, null);
        }

        if (record != null && record.SampleCount > 0)
        {
            return Fail("insufficient_samples", record.SampleCount);
        }

        return Fail("no_data", 0);
    }

    public async Task TrackListingAsync(Listing listing, decimal? soldPrice, string source, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(source, "completed_sale", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!soldPrice.HasValue || soldPrice.Value <= 0)
        {
            return;
        }

        var category = await _uow.SubCategories.Query()
            .Include(sc => sc.Category)
            .Where(sc => sc.SubCategoryId == listing.SubCategoryId)
            .Select(sc => new { sc.CategoryId, CategoryName = sc.Category != null ? sc.Category.Name : null })
            .FirstOrDefaultAsync(cancellationToken);

        var categoryId = category?.CategoryId;
        var productKey = BuildProductKey(listing.Brand, category?.CategoryName, listing.Title);
        if (string.IsNullOrWhiteSpace(productKey))
        {
            return;
        }

        var condition = NormalizeCondition(listing.Condition);
        var record = await _uow.MarketPrices.Query().FirstOrDefaultAsync(x =>
                x.ProductKey == productKey &&
                x.CategoryId == categoryId &&
                x.Condition == condition,
            cancellationToken);

        var price = soldPrice.Value;
        if (await IsOutlierPriceAsync(price, productKey, categoryId, condition, cancellationToken))
        {
            return;
        }
        if (record == null)
        {
            record = new MarketPrice
            {
                ProductKey = productKey,
                CategoryId = categoryId,
                Condition = condition,
                MinPrice = price,
                MaxPrice = price,
                AvgPrice = price,
                SampleCount = 1,
                Source = source,
                Confidence = "LOW",
                UpdatedAt = DateTime.UtcNow
            };
            await _uow.MarketPrices.AddAsync(record, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return;
        }

        var newCount = record.SampleCount + 1;
        record.MinPrice = Math.Min(record.MinPrice, price);
        record.MaxPrice = Math.Max(record.MaxPrice, price);
        record.AvgPrice = ((record.AvgPrice * record.SampleCount) + price) / newCount;
        record.SampleCount = newCount;
        record.Source = source;
        record.Confidence = newCount >= 20 ? "HIGH" : newCount >= SampleThreshold ? "MEDIUM" : "LOW";
        record.UpdatedAt = DateTime.UtcNow;
        _uow.MarketPrices.Update(record);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static MarketPriceResult Fail(string reason, int sampleCount)
        => new(null, null, null, sampleCount, "insufficient_data", "LOW", reason);

    private async Task<bool> IsOutlierPriceAsync(decimal price, string productKey, int? categoryId, string condition, CancellationToken cancellationToken)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddMonths(-6);
            var ordersQuery = _uow.Orders.Query()
                .AsNoTracking()
                .Include(o => o.Listing)
                .ThenInclude(l => l!.SubCategory)
                .ThenInclude(sc => sc!.Category)
                .Where(o => o.Status == "Completed")
                .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value >= fromDate)
                .Where(o => o.Listing != null);

            if (categoryId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.Listing!.SubCategory != null && o.Listing.SubCategory.CategoryId == categoryId.Value);
            }

            var prices = (await ordersQuery.ToListAsync(cancellationToken))
                .Select(o =>
                {
                    var listing = o.Listing!;
                    var categoryName = listing.SubCategory?.Category?.Name;
                    var key = BuildProductKey(listing.Brand, categoryName, listing.Title);
                    var normalizedCondition = NormalizeCondition(listing.Condition);
                    return new { key, normalizedCondition, price = o.TotalAmount ?? 0m };
                })
                .Where(x => x.price > 0)
                .Where(x => x.normalizedCondition == condition && x.key == productKey)
                .Select(x => x.price)
                .OrderBy(x => x)
                .ToList();

            if (prices.Count < 20)
            {
                return false;
            }

            var p5 = Percentile(prices, 0.05);
            var p95 = Percentile(prices, 0.95);
            return price < p5 || price > p95;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outlier filter failed. Price accepted by default.");
            return false;
        }
    }

    private static decimal Percentile(IReadOnlyList<decimal> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        var pos = (sorted.Count - 1) * percentile;
        var lower = (int)Math.Floor(pos);
        var upper = (int)Math.Ceiling(pos);
        if (lower == upper) return sorted[lower];
        var weight = (decimal)(pos - lower);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * weight;
    }

    private static string NormalizeCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return "GOOD";
        var c = condition.Trim().ToUpperInvariant();
        if (c == "EXCELLENT") return "NEW";
        return c is "NEW" or "GOOD" or "FAIR" or "POOR" ? c : "GOOD";
    }

    private static string BuildProductKey(string? brand, string? categoryName, string? title)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(brand)) parts.Add(brand);
        if (!string.IsNullOrWhiteSpace(categoryName)) parts.Add(categoryName);
        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(title)) parts.Add(title);
        return NormalizeKey(string.Join(" ", parts));
    }

    private static string NormalizeKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var trimmed = SpaceRegex.Replace(input, " ").Trim();
        return trimmed.ToLowerInvariant();
    }
}
