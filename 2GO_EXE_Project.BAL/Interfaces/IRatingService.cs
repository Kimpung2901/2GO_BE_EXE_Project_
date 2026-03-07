using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Ratings;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IRatingService
{
    Task<UserRatingResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateUserRatingRequest request, CancellationToken cancellationToken = default);
    Task<UserRatingListResponse> GetRatingsForUserAsync(long userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<UserRatingListResponse> GetMyRatingsAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default);
}
