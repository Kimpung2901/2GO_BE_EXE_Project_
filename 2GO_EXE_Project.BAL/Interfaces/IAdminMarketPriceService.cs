using _2GO_EXE_Project.BAL.DTOs.Ai;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminMarketPriceService
{
    Task<MarketPriceListResponse> GetAllAsync(string? productKey, int? categoryId, string? condition, int skip, int take, CancellationToken cancellationToken = default);
    Task<MarketPriceItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<object> SeedAsync(CancellationToken cancellationToken = default);
    Task<object> BackfillAsync(int monthsBack, decimal minPrice, bool dryRun, CancellationToken cancellationToken = default);
}
