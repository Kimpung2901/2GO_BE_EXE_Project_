namespace _2GO_EXE_Project.BAL.DTOs.Chat;

public record CreateChatRequest(long OtherUserId);

public record ChatThreadResponse(
    long ChatId,
    long OtherUserId,
    string? LastMessage,
    DateTime? LastMessageAt);

public record MessageResponse(
    long MessageId,
    long ChatId,
    long? SenderId,
    string? Content,
    string? ImageUrl,
    DateTime? SentAt);

public record SendMessageRequest(string? Content, string? ImageUrl);
