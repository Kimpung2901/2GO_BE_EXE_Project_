using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<NotificationListResponse> GetMyNotificationsAsync(long userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Notifications.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : Math.Min(take, 100))
            .Select(n => new NotificationItem(
                n.NotificationId,
                n.Title,
                n.Message,
                n.Type,
                n.Link,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new NotificationListResponse(total, items);
    }

    public async Task<BasicResponse> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _uow.Notifications.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, cancellationToken);
        if (notification == null) return new BasicResponse(false, "Không tìm thấy thông báo.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            _uow.Notifications.Update(notification);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return new BasicResponse(true, "Đã đánh dấu đã đọc.");
    }

    public async Task<BasicResponse> MarkAllReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0) return new BasicResponse(true, "Không có thông báo chưa đọc.");

        foreach (var n in notifications)
        {
            n.IsRead = true;
            _uow.Notifications.Update(n);
        }
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "All notifications Đã đánh dấu đã đọc.");
    }

    public async Task<BasicResponse> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0) return new BasicResponse(false, "User id không hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.Title)) return new BasicResponse(false, "Tiêu đề là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.Message)) return new BasicResponse(false, "Nội dung là bắt buộc.");

        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type,
            Link = request.Link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Notifications.AddAsync(notification, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Đã tạo thông báo.");
    }
}




