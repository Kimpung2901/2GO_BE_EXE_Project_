using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.Services;
using _2GO_EXE_Project.DAL.Context;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Implementations;
using System.Threading.Tasks;
using Xunit;

namespace _2GO_EXE_Project.Tests;

public class MarketPriceTrackingTests
{
    [Fact]
    public async Task OnlyCompletedSaleUpdatesMarketPrices()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"market-{Guid.NewGuid()}")
            .Options;

        await using var db = new AppDbContext(options);
        var category = new Category { CategoryId = 1, Name = "Điện thoại" };
        var subCategory = new SubCategory { SubCategoryId = 1, CategoryId = 1, Name = "Smartphone", Category = category };
        await db.Categories.AddAsync(category);
        await db.SubCategories.AddAsync(subCategory);
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db);

        var listing = new Listing
        {
            ListingId = 1,
            Title = "iPhone 12",
            Brand = "Apple",
            SubCategoryId = 1,
            Condition = "GOOD",
            Price = 10000000
        };

        var logger = LoggerFactory.Create(builder => { }).CreateLogger<MarketPriceProvider>();
        var provider = new MarketPriceProvider(uow, logger);

        await provider.TrackListingAsync(listing, listing.Price, "approved_listing");
        Assert.Equal(0, await db.MarketPrices.CountAsync());

        await provider.TrackListingAsync(listing, listing.Price, "completed_sale");
        Assert.Equal(1, await db.MarketPrices.CountAsync());
    }
}
