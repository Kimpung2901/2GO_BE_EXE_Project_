using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/mod/listings")]
[Authorize(Roles = "Admin,Manager")]
public class ModeratorListingsController : ControllerBase
{
    private readonly IModeratorListingService _modListingService;

    public ModeratorListingsController(IModeratorListingService modListingService)
    {
        _modListingService = modListingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetListings([FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _modListingService.GetListingsAsync(status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var listing = await _modListingService.GetByIdAsync(id, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(listing);
    }

    [HttpPut("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken = default)
    {
        var result = await _modListingService.ApproveAsync(User, id, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectListingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modListingService.RejectAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{id:long}/flag")]
    public async Task<IActionResult> Flag(long id, [FromBody] FlagListingRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modListingService.FlagAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
