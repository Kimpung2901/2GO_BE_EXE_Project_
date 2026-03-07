using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _adminUserService.GetUsersAsync(search, role, status, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.CreateUserAsync(User, request, cancellationToken);
        return Ok(user);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetUser(long id, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetUserByIdAsync(id, cancellationToken);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.UpdateUserAsync(User, id, request, cancellationToken);
        return Ok(user);
    }

    [HttpPut("{id:long}/role")]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminUserService.UpdateUserRoleAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminUserService.UpdateUserStatusAsync(User, id, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await _adminUserService.DeleteUserAsync(User, id, cancellationToken);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result);
    }
}
