namespace _2GO_EXE_Project.BAL.DTOs.Ratings;

public record CreateUserRatingRequest(long OrderId, int Score, string? Comment);

public record RaterInfo(long UserId, string? FullName, string? AvatarUrl);

public record UserRatingResponse(
    long RatingId,
    long OrderId,
    long RaterId,
    long RatedUserId,
    int Score,
    string? Comment,
    RaterInfo? Rater,
    DateTime? CreatedAt);

public record UserRatingListResponse(int Total, double? AvgRating, IReadOnlyList<UserRatingResponse> Items);
