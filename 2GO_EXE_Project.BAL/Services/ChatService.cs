using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Chat;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public ChatService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    public async Task<IReadOnlyList<ChatThreadResponse>> GetMyChatsAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var chats = await _uow.Chats.Query()
            .Include(c => c.Messages)
            .Include(c => c.User1)
            .ThenInclude(u => u!.UserProfiles)
            .Include(c => c.User2)
            .ThenInclude(u => u!.UserProfiles)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .ToListAsync(cancellationToken);

        return chats
            .Select(c =>
            {
                var otherUserId = c.User1Id == userId ? c.User2Id : c.User1Id;
                var otherUser = c.User1Id == userId ? c.User2 : c.User1;
                var otherProfile = otherUser?.UserProfiles
                    .OrderBy(p => p.ProfileId)
                    .FirstOrDefault();
                var lastMessage = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();
                return new ChatThreadResponse(
                    c.ChatId,
                    otherUserId ?? 0,
                    otherUser == null
                        ? null
                        : new ChatUserInfo(otherUser.UserId, otherUser.Email, otherProfile?.FullName, otherProfile?.AvatarUrl),
                    lastMessage?.Content ?? lastMessage?.ImageUrl,
                    lastMessage?.SentAt);
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task<ChatThreadResponse> CreateOrGetChatAsync(ClaimsPrincipal userPrincipal, CreateChatRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCreateChat(request));
        var userId = GetUserId(userPrincipal);
        if (request.OtherUserId == userId)
        {
            throw new InvalidOperationException("Bạn không thể chat với chính mình.");
        }

        var existing = await _uow.Chats.Query()
            .Include(c => c.Messages)
            .Include(c => c.User1)
            .ThenInclude(u => u!.UserProfiles)
            .Include(c => c.User2)
            .ThenInclude(u => u!.UserProfiles)
            .FirstOrDefaultAsync(c =>
                    (c.User1Id == userId && c.User2Id == request.OtherUserId) ||
                    (c.User1Id == request.OtherUserId && c.User2Id == userId),
                cancellationToken);

        if (existing != null)
        {
            var otherUser = existing.User1Id == userId ? existing.User2 : existing.User1;
            var otherProfile = otherUser?.UserProfiles
                .OrderBy(p => p.ProfileId)
                .FirstOrDefault();
            var lastMessage = existing.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            return new ChatThreadResponse(
                existing.ChatId,
                request.OtherUserId,
                otherUser == null
                    ? null
                    : new ChatUserInfo(otherUser.UserId, otherUser.Email, otherProfile?.FullName, otherProfile?.AvatarUrl),
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

        var other = await _uow.Users.Query()
            .Include(u => u.UserProfiles)
            .FirstOrDefaultAsync(u => u.UserId == request.OtherUserId, cancellationToken);
        var otherProfileNew = other?.UserProfiles
            .OrderBy(p => p.ProfileId)
            .FirstOrDefault();
        return new ChatThreadResponse(
            chat.ChatId,
            request.OtherUserId,
            other == null ? null : new ChatUserInfo(other.UserId, other.Email, otherProfileNew?.FullName, otherProfileNew?.AvatarUrl),
            null,
            null);
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
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateSendMessage(request));
        var userId = GetUserId(userPrincipal);
        var chat = await _uow.Chats.GetByIdAsync(chatId);
        if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
        {
            return new BasicResponse(false, "Không tìm thấy cuộc trò chuyện.");
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
        var receiverId = chat.User1Id == userId ? chat.User2Id : chat.User1Id;
        if (receiverId.HasValue)
        {
            await NotifyAsync(receiverId.Value, "CHAT", "Tin nhắn mới", "Bạn có tin nhắn mới.", $"/chat/{chatId}", cancellationToken);
        }
        return new BasicResponse(true, "Đã gửi tin nhắn.");
    }

    private async Task NotifyAsync(long userId, string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest(
                userId,
                title,
                message,
                type,
                link), cancellationToken);
        }
        catch
        {
            // ignore notification failures
        }
    }
}








