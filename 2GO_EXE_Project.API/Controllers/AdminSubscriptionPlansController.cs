using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Subscriptions;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/subscription-plans")]
[Authorize(Roles = "Admin")]
public class AdminSubscriptionPlansController : ControllerBase
{
    private readonly IAdminSubscriptionPlanService _service;

    public AdminSubscriptionPlansController(IAdminSubscriptionPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAllAsync(search, isActive, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("{id:int}/audits")]
    public async Task<IActionResult> GetAudits(int id, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAuditsAsync(id, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _service.CreateAsync(request, GetActorUserId(), cancellationToken);
            return Ok(new { planId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _service.UpdateAsync(id, request, GetActorUserId(), cancellationToken);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSubscriptionPlanStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _service.UpdateStatusAsync(id, request, GetActorUserId(), cancellationToken);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}/price")]
    public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdateSubscriptionPlanPriceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _service.UpdatePriceAsync(id, request, GetActorUserId(), cancellationToken);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private long? GetActorUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(ClaimTypes.Name)?.Value;
        return long.TryParse(sub, out var id) ? id : null;
    }
}
