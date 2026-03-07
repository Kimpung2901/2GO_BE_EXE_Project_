namespace _2GO_EXE_Project.BAL.Constants;

public static class ListingNotificationText
{
    public static (string Title, string Message) ForStatus(string? status)
    {
        var normalized = status?.Trim();
        return normalized switch
        {
            ListingStatuses.PendingReview => ("Trạng thái bài đăng", "Bài đăng của bạn đã được gửi duyệt."),
            ListingStatuses.Active => ("Trạng thái bài đăng", "Bài đăng của bạn đã được duyệt thành công."),
            ListingStatuses.Rejected => ("Trạng thái bài đăng", "Bài đăng của bạn chưa đạt yêu cầu và đã bị từ chối."),
            ListingStatuses.Archived => ("Trạng thái bài đăng", "Bài đăng của bạn đã được lưu trữ."),
            ListingStatuses.Deleted => ("Trạng thái bài đăng", "Bài đăng của bạn đã bị xóa theo chính sách."),
            ListingStatuses.Flagged => ("Trạng thái bài đăng", "Bài đăng của bạn đã được gắn cờ."),
            ListingStatuses.Reserved => ("Trạng thái bài đăng", "Bài đăng của bạn đã được đặt chỗ."),
            ListingStatuses.Sold => ("Trạng thái bài đăng", "Bài đăng của bạn đã được đánh dấu đã bán."),
            ListingStatuses.Draft => ("Trạng thái bài đăng", "Bài đăng của bạn đã được lưu vào bản nháp."),
            _ => ("Trạng thái bài đăng", "Trạng thái bài đăng của bạn đã thay đổi.")
        };
    }
}




