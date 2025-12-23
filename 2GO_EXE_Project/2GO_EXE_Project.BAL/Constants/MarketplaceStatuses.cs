namespace _2GO_EXE_Project.BAL.Constants;

public static class ListingStatuses
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string Active = "Active";
    public const string Reserved = "Reserved";
    public const string Sold = "Sold";
    public const string Rejected = "Rejected";
    public const string Archived = "Archived";
    public const string Flagged = "Flagged";
    public const string Deleted = "Deleted";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Draft,
        PendingReview,
        Active,
        Reserved,
        Sold,
        Rejected,
        Archived,
        Flagged,
        Deleted
    };
}

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Disputed = "Disputed";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Paid,
        Failed,
        Cancelled
    };
}

public static class EscrowStatuses
{
    public const string Pending = "Pending";
    public const string Funded = "Funded";
    public const string Holding = "Holding";
    public const string Released = "Released";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
}

public static class ShippingStatuses
{
    public const string Requested = "Requested";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";
}

public static class ReportStatuses
{
    public const string Open = "Open";
    public const string InReview = "InReview";
    public const string WaitingOtherParty = "WaitingOtherParty";
    public const string Resolved = "Resolved";
    public const string Rejected = "Rejected";
}
