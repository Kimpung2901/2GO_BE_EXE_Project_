using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Chat;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyChats(CancellationToken cancellationToken = default)
    {
        var result = await _chatService.GetMyChatsAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _chatService.CreateOrGetChatAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{chatId:long}/messages")]
    public async Task<IActionResult> GetMessages(long chatId, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _chatService.GetMessagesAsync(User, chatId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{chatId:long}/messages")]
    public async Task<IActionResult> SendMessage(long chatId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _chatService.SendMessageAsync(User, chatId, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }
}
