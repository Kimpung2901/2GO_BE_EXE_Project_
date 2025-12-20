using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Chat;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("Invalid user id in token.");
        }
        return id;
    }

    public async Task<IReadOnlyList<ChatThreadResponse>> GetMyChatsAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var chats = await _uow.Chats.Query()
            .Include(c => c.Messages)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .ToListAsync(cancellationToken);

        return chats
            .Select(c =>
            {
                var otherUserId = c.User1Id == userId ? c.User2Id : c.User1Id;
                var lastMessage = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();
                return new ChatThreadResponse(
                    c.ChatId,
                    otherUserId ?? 0,
                    lastMessage?.Content ?? lastMessage?.ImageUrl,
                    lastMessage?.SentAt);
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task<ChatThreadResponse> CreateOrGetChatAsync(ClaimsPrincipal userPrincipal, CreateChatRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        if (request.OtherUserId == userId)
        {
            throw new InvalidOperationException("Cannot chat with yourself.");
        }

        var existing = await _uow.Chats.Query()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId && c.User2Id == request.OtherUserId) ||
                    (c.User1Id == request.OtherUserId && c.User2Id == userId),
                cancellationToken);

        if (existing != null)
        {
            var lastMessage = existing.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            return new ChatThreadResponse(
                existing.ChatId,
                request.OtherUserId,
                lastMessage?.Content ?? lastMessage?.ImageUrl,
                lastMessage?.SentAt);
        }

        var chat = new Chat
        {
            User1Id = userId,
            User2Id = request.OtherUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Chats.AddAsync(chat, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ChatThreadResponse(chat.ChatId, request.OtherUserId, null, null);
    }

    public async Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(ClaimsPrincipal userPrincipal, long chatId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var chat = await _uow.Chats.GetByIdAsync(chatId);
        if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
        {
            return Array.Empty<MessageResponse>();
        }

        var messages = await _uow.Messages.Query()
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(m => new MessageResponse(m.MessageId, m.ChatId ?? 0, m.SenderId, m.Content, m.ImageUrl, m.SentAt))
            .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task<BasicResponse> SendMessageAsync(ClaimsPrincipal userPrincipal, long chatId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var chat = await _uow.Chats.GetByIdAsync(chatId);
        if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
        {
            return new BasicResponse(false, "Chat not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return new BasicResponse(false, "Message content or image is required.");
        }

        var message = new Message
        {
            ChatId = chatId,
            SenderId = userId,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            SentAt = DateTime.UtcNow
        };

        await _uow.Messages.AddAsync(message, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Message sent.");
    }
}
