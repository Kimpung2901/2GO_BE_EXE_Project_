namespace _2GO_EXE_Project.BAL.DTOs.Reports;

public record CreateReportRequest(long? ListingId, long? TargetUserId, string Reason);

public record ReportResponse(
    long ReportId,
    long? ListingId,
    long? TargetUserId,
    string? Reason,
    string? Status,
    DateTime? CreatedAt);

public record ReportListResponse(int Total, IReadOnlyList<ReportResponse> Items);
