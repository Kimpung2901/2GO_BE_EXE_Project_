using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Shipping;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/shipping")]
[Authorize]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShippingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shippingService.CreateAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("order/{orderId:long}")]
    public async Task<IActionResult> GetByOrder(long orderId, CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.GetByOrderAsync(User, orderId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{shipId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long shipId, [FromBody] UpdateShippingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.UpdateStatusAsync(User, shipId, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
