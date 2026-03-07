// Thêm constants này vào class ErrorMessages hiện có (hoặc tạo mới nếu chưa có)
namespace _2GO_EXE_Project.BAL.Constants;

public static class ErrorMessages
{
    // Comment errors
    public const string INVALID_LISTING = "INVALID_LISTING";
    public const string LISTING_NOT_FOUND = "Không tìm thấy bài đăng.";

    public const string INVALID_COMMENT = "INVALID_COMMENT";
    public const string COMMENT_NOT_FOUND = "Không tìm thấy bình luận.";

    public const string INVALID_PARENT_COMMENT = "INVALID_PARENT_COMMENT";
    public const string PARENT_COMMENT_NOT_FOUND = "Parent không tìm thấy bình luận.";
    public const string PARENT_COMMENT_DIFFERENT_LISTING = "Bình luận cha thuộc về bài đăng khác.";

    public const string FORBIDDEN = "FORBIDDEN";
    public const string NOT_COMMENT_OWNER = "Bạn không phải là chủ bình luận này.";
}
