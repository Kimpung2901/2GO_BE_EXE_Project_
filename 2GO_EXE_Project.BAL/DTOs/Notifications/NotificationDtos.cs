namespace _2GO_EXE_Project.BAL.DTOs.Notifications;

public record NotificationItem(
    long NotificationId,
    string? Title,
    string? Message,
    string? Type,
    string? Link,
    bool IsRead,
    DateTime? CreatedAt);

public record NotificationListResponse(int Total, IReadOnlyList<NotificationItem> Items);

public record CreateNotificationRequest(
    long UserId,
    string Title,
    string Message,
    string? Type,
    string? Link);