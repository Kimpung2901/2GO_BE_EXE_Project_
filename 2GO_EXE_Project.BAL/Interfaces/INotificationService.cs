using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Notifications;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetMyNotificationsAsync(long userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<BasicResponse> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default);
    Task<BasicResponse> MarkAllReadAsync(long userId, CancellationToken cancellationToken = default);
    Task<BasicResponse> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
}