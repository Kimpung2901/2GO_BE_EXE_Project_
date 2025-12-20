using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingsController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? subCategoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? status,
        [FromQuery] string? condition,
        [FromQuery] string? brand,
        [FromQuery] int? wardId,
        [FromQuery] int? districtId,
        [FromQuery] int? cityId,
        [FromQuery] bool? hasNegotiation,
        [FromQuery] string? sort,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _listingService.GetListingsAsync(
            search,
            categoryId,
            subCategoryId,
            minPrice,
            maxPrice,
            status,
            condition,
            brand,
            wardId,
            districtId,
            cityId,
            hasNegotiation,
            sort,
            skip,
            take,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrNull();
        var listing = await _listingService.GetListingByIdAsync(id, true, userId, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(listing);
    }

    private long? GetUserIdOrNull()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }
}
