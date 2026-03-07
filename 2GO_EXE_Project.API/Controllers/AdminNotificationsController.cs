using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.CreateAsync(request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}