using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Admin;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/payouts")]
[Authorize(Roles = "Admin")]
public class AdminPayoutsController : ControllerBase
{
    private readonly IAdminPayoutService _payoutService;

    public AdminPayoutsController(IAdminPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    [HttpGet("forfeit")]
    public async Task<IActionResult> GetForfeitPayouts(
        [FromQuery] string? status,
        [FromQuery] long? sellerId,
        [FromQuery] long? orderId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _payoutService.GetForfeitPayoutsAsync(status, sellerId, orderId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forfeit/retry")]
    public async Task<IActionResult> RetryForfeitPayout([FromBody] RetryPayoutRequest request, CancellationToken cancellationToken = default)
    {
        var success = await _payoutService.RetryForfeitPayoutAsync(request.EscrowId, cancellationToken);
        return Ok(new { success });
    }
}
