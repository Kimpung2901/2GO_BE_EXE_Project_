namespace _2GO_EXE_Project.BAL.DTOs.Reports;

public record CreateReportRequest(long OrderId, long? TargetUserId, string Reason, IReadOnlyList<string>? EvidenceUrls);

public record ReplyReportRequest(string Message);

public record ReportResponse(
    long ReportId,
    long OrderId,
    long? ReporterId,
    long? TargetUserId,
    string? Reason,
    IReadOnlyList<string>? EvidenceUrls,
    string? Status,
    long? WaitingForUserId,
    DateTime? CreatedAt);

public record ReportListResponse(int Total, IReadOnlyList<ReportResponse> Items);
