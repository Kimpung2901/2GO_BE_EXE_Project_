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

public static class ListingTypes
{
    public const string Single = "SINGLE";
    public const string Multi = "MULTI";

    public static readonly IReadOnlyList<string> All = new[] { Single, Multi };
}

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Delivering = "Delivering";
    public const string Delivered = "Delivered";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Disputed = "Disputed";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Confirmed,
        Delivering,
        Delivered,
        Completed,
        Cancelled,
        Disputed
    };
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

public static class PaymentMethods
{
    public const string COD = "COD";
    public const string PAYOS = "PAYOS";

    public static readonly IReadOnlyList<string> All = new[]
    {
        COD,
        PAYOS
    };
}

public static class PaymentTypes
{
    public const string Commission = "COMMISSION";
    public const string Subscription = "SUBSCRIPTION";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Commission,
        Subscription
    };
}

public static class PaymentStages
{
    public const string Deposit = "DEPOSIT";
    public const string Remaining = "REMAINING";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Deposit,
        Remaining
    };
}

public static class EscrowRules
{
    public const decimal DepositRate = 0.10m;
    public const decimal DepositThresholdAmount = 2_000_000m;
    public const int DepositHoldHours = 72;
}

public static class OrderRules
{
    public const int SellerConfirmHoldHours = 48;
}

public static class EscrowStatuses
{
    public const string Pending = "Pending";
    public const string Funded = "Funded";
    public const string Holding = "Holding";
    public const string Released = "Released";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Funded,
        Holding,
        Released,
        Cancelled,
        Refunded
    };
}

public static class ShippingStatuses
{
    public const string Requested = "Requested";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Requested,
        InTransit,
        Delivered,
        Failed
    };
}

public static class ReportStatuses
{
    public const string Open = "Open";
    public const string InReview = "InReview";
    public const string WaitingOtherParty = "WaitingOtherParty";
    public const string Resolved = "Resolved";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Open,
        InReview,
        WaitingOtherParty,
        Resolved,
        Rejected
    };
}

public static class UserStatuses
{
    public const string Active = "Active";
    public const string Banned = "Banned";
    public const string Deleted = "Deleted";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Active,
        Banned,
        Deleted
    };
}

public static class CartStatuses
{
    public const string Active = "ACTIVE";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Active
    };
}

public static class CartItemStatuses
{
    public const string Available = "AVAILABLE";
    public const string Unavailable = "UNAVAILABLE";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Available,
        Unavailable
    };
}
