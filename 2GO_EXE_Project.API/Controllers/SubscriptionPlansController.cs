using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Subscriptions;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/subscription-plans")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;
    private readonly IAuthService _authService;

    public SubscriptionPlansController(ISubscriptionPlanService planService, IAuthService authService)
    {
        _planService = planService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken = default)
    {
        var result = await _planService.GetActiveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken = default)
    {
        var result = await _authService.GetMySubscriptionAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me/usage")]
    [Authorize]
    public async Task<IActionResult> GetMySubscriptionUsage(CancellationToken cancellationToken = default)
    {
        var result = await _authService.GetMySubscriptionUsageAsync(User, cancellationToken);
        return Ok(result);
    }
}
