using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Chat;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<ChatThreadResponse>> GetMyChatsAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<ChatThreadResponse> CreateOrGetChatAsync(ClaimsPrincipal userPrincipal, CreateChatRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(ClaimsPrincipal userPrincipal, long chatId, int skip, int take, CancellationToken cancellationToken = default);
    Task<BasicResponse> SendMessageAsync(ClaimsPrincipal userPrincipal, long chatId, SendMessageRequest request, CancellationToken cancellationToken = default);
}
