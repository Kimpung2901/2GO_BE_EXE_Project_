using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/ai/listings")]
public class AiListingsController : ControllerBase
{
    private readonly IAiListingService _aiListingService;

    public AiListingsController(IAiListingService aiListingService)
    {
        _aiListingService = aiListingService;
    }

    [HttpPost("analyze")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Analyze([FromBody] AiListingAnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aiListingService.AnalyzeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (IsGeminiQuotaError(ex))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "Đã vượt quá quota Gemini. Vui lòng thử lại sau.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("precheck")]
    public async Task<IActionResult> Precheck([FromBody] AiListingPrecheckRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _aiListingService.PrecheckAsync(request, deepChecks: false, cancellationToken);
        return Ok(result);
    }

    private static bool IsGeminiQuotaError(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("API request failed", StringComparison.OrdinalIgnoreCase);
    }
}


