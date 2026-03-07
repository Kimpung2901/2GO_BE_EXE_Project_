using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Comments;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateAsync(ClaimsPrincipal userPrincipal, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentListResponse> GetByListingIdAsync(long listingId, int skip, int take, CancellationToken cancellationToken = default);
    Task<CommentDto?> GetByIdAsync(long commentId, CancellationToken cancellationToken = default);
    Task<CommentListResponse> GetRepliesAsync(long listingId, long parentId, int skip, int take, CancellationToken cancellationToken = default);
    Task<CommentDto> UpdateAsync(ClaimsPrincipal userPrincipal, long commentId, UpdateCommentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(ClaimsPrincipal userPrincipal, long commentId, CancellationToken cancellationToken = default);
}
