namespace _2GO_EXE_Project.BAL.Constants;

public static class ReportNotificationText
{
    public static (string Title, string Message) ForStatus(string? status, long reportId)
    {
        var normalized = status?.Trim();
        return normalized switch
        {
            ReportStatuses.WaitingOtherParty => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đang chờ phản hồi từ bạn."),
            ReportStatuses.Resolved => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đã được giải quyết."),
            ReportStatuses.Rejected => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đã bị từ chối."),
            ReportStatuses.InReview => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đang được xem xét."),
            ReportStatuses.Open => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đã được mở."),
            _ => ("Cập nhật báo cáo", $"Báo cáo #{reportId} đã được cập nhật.")
        };
    }
}




