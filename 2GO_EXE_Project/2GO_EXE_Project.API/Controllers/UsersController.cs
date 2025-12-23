using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _authService.GetCurrentUserAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.UpdateCurrentUserProfileAsync(User, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ChangePasswordAsync(User, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }
        return Ok(result);
    }

    [HttpGet("me/devices")]
    public async Task<IActionResult> GetDevices(CancellationToken cancellationToken)
    {
        var result = await _authService.GetMyDevicesAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("me/devices/{deviceId:long}")]
    public async Task<IActionResult> RemoveDevice(long deviceId, CancellationToken cancellationToken)
    {
        var result = await _authService.RemoveMyDeviceAsync(User, deviceId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }
        return Ok(result);
    }

    [HttpGet("me/activity")]
    public async Task<IActionResult> GetActivity(CancellationToken cancellationToken)
    {
        var result = await _authService.GetMyActivityAsync(User, cancellationToken);
        return Ok(result);
    }
}
