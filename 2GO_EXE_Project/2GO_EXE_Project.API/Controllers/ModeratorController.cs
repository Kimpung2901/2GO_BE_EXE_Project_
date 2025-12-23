using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/mod")]
[Authorize(Roles = "Admin,Manager")]
public class ModeratorController : ControllerBase
{
    private readonly IModeratorService _modService;

    public ModeratorController(IModeratorService modService)
    {
        _modService = modService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _modService.GetUsersAsync(search, status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPut("users/{id:long}/ban")]
    public async Task<IActionResult> BanUser(long id, [FromBody] BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modService.BanUserAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("users/{id:long}/unban")]
    public async Task<IActionResult> UnbanUser(long id, CancellationToken cancellationToken = default)
    {
        var result = await _modService.UnbanUserAsync(User, id, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _modService.GetReportsAsync(status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("reports/{id:long}")]
    public async Task<IActionResult> GetReport(long id, CancellationToken cancellationToken = default)
    {
        var report = await _modService.GetReportByIdAsync(id, cancellationToken);
        if (report == null) return NotFound();
        return Ok(report);
    }

    [HttpPut("reports/{id:long}/resolve")]
    public async Task<IActionResult> ResolveReport(long id, [FromBody] ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _modService.ResolveReportAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
