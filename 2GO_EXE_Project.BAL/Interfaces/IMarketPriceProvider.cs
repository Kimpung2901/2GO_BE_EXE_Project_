using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IMarketPriceProvider
{
    Task<MarketPriceResult> GetMarketPriceAsync(MarketPriceInput input, CancellationToken cancellationToken = default);
    Task TrackListingAsync(Listing listing, decimal? soldPrice, string source, CancellationToken cancellationToken = default);
}
