using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dashboardService.GetSummaryAsync(from, to, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("timeseries")]
    public async Task<IActionResult> GetTimeseries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string bucket = "day",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dashboardService.GetTimeseriesAsync(from, to, bucket, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
