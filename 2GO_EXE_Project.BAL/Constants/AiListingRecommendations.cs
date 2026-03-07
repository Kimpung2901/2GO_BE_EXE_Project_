namespace _2GO_EXE_Project.BAL.Constants;

public static class AiListingRecommendations
{
    public const string Published = "PUBLISHED";
    public const string PendingReview = "PENDING_REVIEW";
    public const string RejectedDraft = "REJECTED_DRAFT";

    public static string ToListingStatus(string recommendation, bool useDraftForRejected = false)
    {
        if (string.IsNullOrWhiteSpace(recommendation))
        {
            return ListingStatuses.PendingReview;
        }

        return recommendation switch
        {
            Published => ListingStatuses.Active,
            PendingReview => ListingStatuses.PendingReview,
            RejectedDraft => useDraftForRejected ? ListingStatuses.Draft : ListingStatuses.Rejected,
            _ => ListingStatuses.PendingReview
        };
    }
}
