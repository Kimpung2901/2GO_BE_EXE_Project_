using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Media;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(ICloudinaryService cloudinaryService, ILogger<UploadsController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpPost("image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CloudinaryUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File là bắt buộc.");
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Chỉ cho phép file ảnh.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _cloudinaryService.UploadImageAsync(stream, file.FileName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload image failed. FileName={FileName}, ContentType={ContentType}, Length={Length}",
                file.FileName, file.ContentType, file.Length);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPost("video")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CloudinaryUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadVideo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File là bắt buộc.");
        }

        if (!file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Chỉ cho phép file video.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _cloudinaryService.UploadVideoAsync(stream, file.FileName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload video failed. FileName={FileName}, ContentType={ContentType}, Length={Length}",
                file.FileName, file.ContentType, file.Length);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}




