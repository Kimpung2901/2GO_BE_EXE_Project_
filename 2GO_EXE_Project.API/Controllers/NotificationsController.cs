using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(User);
        var result = await _notificationService.GetMyNotificationsAsync(userId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(User);
        var result = await _notificationService.MarkReadAsync(userId, id, cancellationToken);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(User);
        var result = await _notificationService.MarkAllReadAsync(userId, cancellationToken);
        return Ok(result);
    }
}
