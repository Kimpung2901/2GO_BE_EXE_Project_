using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Reports;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _reportService.CreateAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyReports([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetMyReportsAsync(User, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{reportId:long}/reply")]
    public async Task<IActionResult> Reply(long reportId, [FromBody] ReplyReportRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _reportService.ReplyAsync(User, reportId, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
