namespace _2GO_EXE_Project.BAL.DTOs.Comments;

public record CreateCommentRequest(
    long ListingId,
    string Content,
    long? ParentId = null);

public record UpdateCommentRequest(string Content);

public record CommentDto(
    long CommentId,
    long ListingId,
    long UserId,
    string? UserName,
    string? UserAvatarUrl,
    string Content,
    long? ParentId,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    int ReplyCount);

public record CommentListResponse(int Total, IReadOnlyList<CommentDto> Items);