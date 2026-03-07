using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/market-prices")]
[Authorize(Roles = "Admin")]
public class AdminMarketPricesController : ControllerBase
{
    private readonly IAdminMarketPriceService _service;

    public AdminMarketPricesController(IAdminMarketPriceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? productKey,
        [FromQuery] int? categoryId,
        [FromQuery] string? condition,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAllAsync(productKey, categoryId, condition, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken = default)
    {
        var result = await _service.SeedAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill(
        [FromQuery] int monthsBack = 6,
        [FromQuery] decimal minPrice = 100_000,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.BackfillAsync(monthsBack, minPrice, dryRun, cancellationToken);
        return Ok(result);
    }
}
