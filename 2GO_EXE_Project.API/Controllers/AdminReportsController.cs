using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IModeratorService _moderatorService;

    public AdminReportsController(IModeratorService moderatorService)
    {
        _moderatorService = moderatorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderatorService.GetReportsAsync(status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var report = await _moderatorService.GetReportByIdAsync(id, cancellationToken);
        if (report == null) return NotFound();
        return Ok(report);
    }

    [HttpPut("{id:long}/resolve")]
    public async Task<IActionResult> Resolve(long id, [FromBody] ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _moderatorService.ResolveReportAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
