using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/seller/listings")]
[Authorize]
public class SellerListingsController : ControllerBase
{
    private readonly ISellerListingService _sellerListingService;

    public SellerListingsController(ISellerListingService sellerListingService)
    {
        _sellerListingService = sellerListingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyListings([FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sellerListingService.GetMyListingsAsync(User, status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var listing = await _sellerListingService.GetMyListingByIdAsync(User, id, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(listing);
    }

    [HttpGet("{id:long}/stats")]
    public async Task<IActionResult> GetStats(long id, CancellationToken cancellationToken = default)
    {
        var stats = await _sellerListingService.GetMyListingStatsAsync(User, id, cancellationToken);
        if (stats == null) return NotFound();
        return Ok(stats);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sellerListingService.CreateAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSellerListingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sellerListingService.UpdateAsync(User, id, request, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:long}/publish")]
    public async Task<IActionResult> Publish(long id, CancellationToken cancellationToken = default)
    {
        var result = await _sellerListingService.PublishAsync(User, id, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken = default)
    {
        var result = await _sellerListingService.ArchiveAsync(User, id, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{id:long}/images")]
    public async Task<IActionResult> UpdateImages(long id, [FromBody] UpdateListingImagesRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sellerListingService.UpdateImagesAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
