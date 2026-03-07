using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService;

    public AdminOrdersController(IAdminOrderService adminOrderService)
    {
        _adminOrderService = adminOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] long? buyerId,
        [FromQuery] long? sellerId,
        [FromQuery] long? orderCode,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminOrderService.GetOrdersAsync(status, buyerId, sellerId, orderCode, from, to, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{orderId:long}")]
    public async Task<IActionResult> GetById(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _adminOrderService.GetByIdAsync(orderId, cancellationToken);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPut("{orderId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _adminOrderService.UpdateStatusAsync(User, orderId, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
