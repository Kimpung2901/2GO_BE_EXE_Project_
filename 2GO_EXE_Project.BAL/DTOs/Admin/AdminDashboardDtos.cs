namespace _2GO_EXE_Project.BAL.DTOs.Admin;

public record AdminPlanCount(
    string Code,
    string Name,
    int Users);

public record AdminKpiSummary(
    decimal GmvCompleted,
    int OrdersTotal,
    int OrdersCompleted,
    int OrdersCancelled,
    int ListingsNew,
    int ListingsActive,
    int ListingsPendingReview,
    int UsersNew,
    int PaymentsPaid,
    int PaymentsFailed,
    decimal SubscriptionRevenue,
    decimal CommissionRevenue,
    decimal EscrowHeldAmount,
    decimal EscrowReleasedAmount,
    decimal OrdersCancelledRate,
    decimal PaymentsFailedRate,
    IReadOnlyList<AdminPlanCount> UsersByPlan,
    int ReportsNew);

public record AdminDashboardResponse(
    DateTime From,
    DateTime To,
    AdminKpiSummary Summary);
