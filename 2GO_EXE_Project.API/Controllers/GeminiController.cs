using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/ai/gemini")]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _geminiService;

    public GeminiController(IGeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeminiGenerateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { message = "Prompt là bắt buộc." });
        }

        var text = await _geminiService.GenerateAsync(request.Prompt, null, cancellationToken);
        return Ok(new GeminiGenerateResponse(text));
    }
}



