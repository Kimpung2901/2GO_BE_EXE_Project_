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
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int? subCategoryId, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _listingService.GetListingsAsync(search, categoryId, subCategoryId, minPrice, maxPrice, status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var listing = await _listingService.GetListingByIdAsync(id, true, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(listing);
    }
}
