using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Escrow;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/escrows")]
[Authorize]
public class EscrowsController : ControllerBase
{
    private readonly IEscrowService _escrowService;

    public EscrowsController(IEscrowService escrowService)
    {
        _escrowService = escrowService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEscrowRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _escrowService.CreateAsync(User, request, cancellationToken);
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
        var result = await _escrowService.GetByOrderAsync(User, orderId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{escrowId:long}/transactions")]
    public async Task<IActionResult> AddTransaction(long escrowId, [FromBody] CreateEscrowTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _escrowService.AddTransactionAsync(User, escrowId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{escrowId:long}/release")]
    public async Task<IActionResult> Release(long escrowId, CancellationToken cancellationToken = default)
    {
        var result = await _escrowService.ReleaseAsync(User, escrowId, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
