using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Comments;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/listings/{listingId:long}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        long listingId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure listingId matches
            if (request.ListingId != listingId)
            {
                return BadRequest("ListingId không khợp.");
            }

            var result = await _commentService.CreateAsync(User, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { listingId, commentId = result.CommentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetByListing(
        long listingId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetByListingIdAsync(listingId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{commentId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long listingId, long commentId, CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetByIdAsync(commentId, cancellationToken);
        if (result == null || result.ListingId != listingId)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{commentId:long}/replies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReplies(
        long listingId,
        long commentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _commentService.GetRepliesAsync(listingId, commentId, skip, take, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPatch("{commentId:long}")]
    public async Task<IActionResult> Update(
        long listingId,
        long commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _commentService.UpdateAsync(User, commentId, request, cancellationToken);
            if (result.ListingId != listingId)
            {
                return NotFound();
            }
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{commentId:long}")]
    public async Task<IActionResult> Delete(long listingId, long commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _commentService.DeleteAsync(User, commentId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}



