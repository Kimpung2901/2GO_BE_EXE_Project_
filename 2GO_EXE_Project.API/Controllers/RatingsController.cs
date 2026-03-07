using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Ratings;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRatingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _ratingService.CreateAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyRatings([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _ratingService.GetMyRatingsAsync(User, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{userId:long}")]
    public async Task<IActionResult> GetRatingsForUser(long userId, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _ratingService.GetRatingsForUserAsync(userId, skip, take, cancellationToken);
        return Ok(result);
    }
}
