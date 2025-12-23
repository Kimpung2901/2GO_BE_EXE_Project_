namespace _2GO_EXE_Project.BAL.DTOs.Reports;

public record CreateReportRequest(long OrderId, long? TargetUserId, string Reason);

public record ReplyReportRequest(string Message);

public record ReportResponse(
    long ReportId,
    long OrderId,
    long? ReporterId,
    long? TargetUserId,
    string? Reason,
    string? Status,
    long? WaitingForUserId,
    DateTime? CreatedAt);

public record ReportListResponse(int Total, IReadOnlyList<ReportResponse> Items);
