using _2GO_EXE_Project.BAL.DTOs.Media;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
