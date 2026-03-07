namespace _2GO_EXE_Project.BAL.DTOs.Admin;

public record AdminTimeseriesPoint(
    DateTime PeriodStart,
    decimal GmvCompleted,
    int OrdersTotal,
    int OrdersCompleted,
    int OrdersCancelled,
    int ListingsNew,
    int UsersNew,
    int PaymentsPaid,
    int PaymentsFailed,
    decimal SubscriptionRevenue,
    decimal CommissionRevenue,
    decimal EscrowHeldAmount,
    decimal EscrowReleasedAmount,
    int ReportsNew);

public record AdminTimeseriesResponse(
    DateTime From,
    DateTime To,
    string Bucket,
    IReadOnlyList<AdminTimeseriesPoint> Points);
