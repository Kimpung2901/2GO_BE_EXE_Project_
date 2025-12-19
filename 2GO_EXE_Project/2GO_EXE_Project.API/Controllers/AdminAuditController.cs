using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin")]
public class AdminAuditController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminAuditController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _adminUserService.GetAuditLogsAsync(skip, take, cancellationToken);
        return Ok(logs);
    }
}
