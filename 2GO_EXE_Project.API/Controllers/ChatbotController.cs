using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Chatbot;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    private static long? TryGetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (long.TryParse(sub, out var id)) return id;
        return null;
    }

    [HttpPost("ask")]
    [AllowAnonymous]
    public async Task<IActionResult> Ask([FromBody] ChatbotAskRequest request, CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId(User);
        var result = await _chatbotService.AskAsync(userId, request, cancellationToken);
        return Ok(result);
    }
}