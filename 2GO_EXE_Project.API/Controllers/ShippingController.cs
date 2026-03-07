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

    [HttpPost("ghn")]
    public async Task<IActionResult> CreateGhn([FromBody] CreateGhnShippingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shippingService.CreateGhnAsync(User, request, cancellationToken);
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

    [HttpGet("ghn/provinces")]
    public async Task<IActionResult> GetGhnProvinces(CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.GetGhnProvincesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("ghn/districts")]
    public async Task<IActionResult> GetGhnDistricts([FromQuery] int provinceId, CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.GetGhnDistrictsAsync(provinceId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ghn/wards")]
    public async Task<IActionResult> GetGhnWards([FromQuery] int districtId, CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.GetGhnWardsAsync(districtId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ghn/fee")]
    public async Task<IActionResult> GetGhnFee([FromBody] GhnFeeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _shippingService.GetGhnFeeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ghn/cancel")]
    public async Task<IActionResult> CancelGhn([FromBody] GhnCancelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shippingService.CancelGhnAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("ghn/print-token")]
    public async Task<IActionResult> GetGhnPrintToken([FromBody] GhnPrintTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shippingService.GetGhnPrintTokenAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ghn/order-info")]
    public async Task<IActionResult> GetGhnOrderInfo([FromQuery] string orderCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shippingService.GetGhnOrderInfoAsync(User, orderCode, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("ghn/webhook")]
    public async Task<IActionResult> GhnWebhook([FromBody] GhnWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        var token = Request.Headers["Token"].FirstOrDefault()
                    ?? Request.Headers["X-GHN-Token"].FirstOrDefault()
                    ?? Request.Headers["X-Webhook-Token"].FirstOrDefault();
        var result = await _shippingService.HandleGhnWebhookAsync(payload, token, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
