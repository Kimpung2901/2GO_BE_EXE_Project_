namespace _2GO_EXE_Project.BAL.Constants;

public static class OrderNotificationText
{
    public static (string Title, string Message) ForStatus(string? status, long orderId)
    {
        var normalized = status?.Trim();
        return normalized switch
        {
            OrderStatuses.Pending => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đang chờ xử lý."),
            OrderStatuses.Confirmed => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đã được xác nhận."),
            OrderStatuses.Delivering => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đang được giao."),
            OrderStatuses.Delivered => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đã giao."),
            OrderStatuses.Completed => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đã hoàn tất."),
            OrderStatuses.Cancelled => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đã bị hủy."),
            OrderStatuses.Disputed => ("Trạng thái đơn hàng", $"Đơn hàng #{orderId} đang tranh chấp."),
            _ => ("Trạng thái đơn hàng", $"Trạng thái đơn hàng #{orderId} đã được cập nhật.")
        };
    }
}




