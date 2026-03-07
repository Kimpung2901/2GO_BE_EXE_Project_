using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Services;
using _2GO_EXE_Project.DAL.Context;
using _2GO_EXE_Project.DAL.Repositories.Implementations;
using System.Threading.Tasks;
using Xunit;

namespace _2GO_EXE_Project.Tests;

public class PricingTests
{
    [Fact]
    public async Task NoMarketData_ReturnsLowConfidenceAndNoSuggestedRange()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pricing-{Guid.NewGuid()}")
            .Options;

        await using var db = new AppDbContext(options);
        using var uow = new UnitOfWork(db);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<MarketPriceProvider>();
        var marketProvider = new MarketPriceProvider(uow, logger);

        var market = await marketProvider.GetMarketPriceAsync(new MarketPriceInput("iphone 12", null, "GOOD"));
        var pricing = new AiPricingResult("iphone 12", market.MarketAvg, market.Confidence, "GOOD", null, null);
        var pricingService = new PricingService();
        var result = pricingService.BuildSuggestedRange(pricing);

        Assert.Equal("LOW", market.Confidence);
        Assert.Null(market.MarketAvg);
        Assert.Null(result.SuggestedMin);
        Assert.Null(result.SuggestedMax);
    }
}
