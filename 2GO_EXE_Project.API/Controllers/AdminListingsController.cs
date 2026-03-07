using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/listings")]
[Authorize(Roles = "Admin")]
public class AdminListingsController : ControllerBase
{
    private readonly IAdminListingService _adminListingService;

    public AdminListingsController(IAdminListingService adminListingService)
    {
        _adminListingService = adminListingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetListings(
        [FromQuery] string? status,
        [FromQuery] long? sellerId,
        [FromQuery] int? categoryId,
        [FromQuery] int? subCategoryId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminListingService.GetListingsAsync(status, sellerId, categoryId, subCategoryId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var listing = await _adminListingService.GetByIdAsync(id, cancellationToken);
        if (listing == null) return NotFound();
        return Ok(listing);
    }

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateListingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _adminListingService.UpdateStatusAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var result = await _adminListingService.DeleteAsync(User, id, cancellationToken);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result);
    }
}
