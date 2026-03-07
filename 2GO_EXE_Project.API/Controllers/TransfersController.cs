using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Transfers;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize(Roles = "Admin, Manager")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.CreateTransferAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Tạo chuyển khoản thất bại", error = ex.Message });
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatchTransfer([FromBody] CreateBatchTransferRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.CreateBatchTransferAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Tạo chuyển khoản hàng loạt thất bại", error = ex.Message });
        }
    }

    [HttpGet("{transferId:long}")]
    public async Task<IActionResult> GetTransfer(long transferId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.GetTransferByIdAsync(User, transferId, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy giao dịch" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lấy thông tin chuyển khoản thất bại", error = ex.Message });
        }
    }

    [HttpGet("reference/{referenceId}")]
    public async Task<IActionResult> GetTransferByReference(string referenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.GetTransferByReferenceIdAsync(User, referenceId, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy giao dịch" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lấy thông tin chuyển khoản thất bại", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserTransfers(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.GetUserTransfersAsync(User, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lấy danh sách chuyển khoản thất bại", error = ex.Message });
        }
    }

    [HttpGet("account-balance")]
    public async Task<IActionResult> GetAccountBalance(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.GetAccountBalanceAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lấy số dư tài khoản thất bại", error = ex.Message });
        }
    }

    [HttpPost("estimate-credit")]
    public async Task<IActionResult> EstimateCredit([FromBody] EstimateCreditRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferService.EstimateCreditAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ước tính hạn mức thất bại", error = ex.Message });
        }
    }
}

