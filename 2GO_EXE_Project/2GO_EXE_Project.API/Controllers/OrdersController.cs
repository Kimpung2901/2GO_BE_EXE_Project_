using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.CreateAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetMyOrdersAsync(User, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{orderId:long}")]
    public async Task<IActionResult> GetById(long orderId, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetByIdAsync(User, orderId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{orderId:long}/cancel")]
    public async Task<IActionResult> Cancel(long orderId, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.CancelAsync(User, orderId, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{orderId:long}/confirm")]
    public async Task<IActionResult> Confirm(long orderId, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.ConfirmAsync(User, orderId, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpPut("{orderId:long}/complete")]
    public async Task<IActionResult> Complete(long orderId, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.CompleteAsync(User, orderId, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
