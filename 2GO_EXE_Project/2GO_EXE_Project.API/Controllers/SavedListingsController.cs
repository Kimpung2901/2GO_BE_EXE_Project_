using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/saved-listings")]
[Authorize]
public class SavedListingsController : ControllerBase
{
    private readonly ISavedListingService _savedListingService;

    public SavedListingsController(ISavedListingService savedListingService)
    {
        _savedListingService = savedListingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMySaved([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _savedListingService.GetMySavedAsync(User, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{listingId:long}")]
    public async Task<IActionResult> GetSavedStatus(long listingId, CancellationToken cancellationToken = default)
    {
        var result = await _savedListingService.GetSavedStatusAsync(User, listingId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveListingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _savedListingService.SaveAsync(User, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpDelete("{listingId:long}")]
    public async Task<IActionResult> Remove(long listingId, CancellationToken cancellationToken = default)
    {
        var result = await _savedListingService.RemoveAsync(User, listingId, cancellationToken);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result);
    }
}
