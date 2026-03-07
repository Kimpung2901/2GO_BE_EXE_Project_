using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.DTOs.Media;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException("Thiếu cấu hình Cloudinary.");
        }

        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream)
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException("Tải lên Cloudinary thất bại.");
        }

        return new CloudinaryUploadResult(
            result.PublicId ?? string.Empty,
            result.Url?.ToString() ?? string.Empty,
            result.SecureUrl?.ToString() ?? string.Empty);
    }

    public async Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, fileStream)
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException("Tải lên Cloudinary thất bại.");
        }

        return new CloudinaryUploadResult(
            result.PublicId ?? string.Empty,
            result.Url?.ToString() ?? string.Empty,
            result.SecureUrl?.ToString() ?? string.Empty);
    }
}
