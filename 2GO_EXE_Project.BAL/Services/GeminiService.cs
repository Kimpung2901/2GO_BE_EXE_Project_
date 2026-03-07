using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;
    private static readonly object RateLock = new();
    private static readonly Dictionary<string, Queue<DateTime>> RequestsPerMinute = new();
    private static readonly Dictionary<string, Queue<DateTime>> RequestsPerDay = new();

    public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new GeminiSettings();
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, string? userKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình Gemini API key.");
        }

        if (!TryConsumeQuota(DateTime.UtcNow, userKey, _settings.MaxRequestsPerMinute, _settings.MaxRequestsPerDay))
        {
            throw new InvalidOperationException("Gemini vượt quá giới hạn truy cập.");
        }

        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/models/{_settings.Model}:generateContent";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Yêu cầu Gemini API thất bại.");
        }

        return ExtractText(body);
    }

    public async Task<string> GenerateFromImageAsync(string prompt, string imageUrl, string? userKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình Gemini API key.");
        }

        if (!TryConsumeQuota(DateTime.UtcNow, userKey, _settings.MaxRequestsPerMinute, _settings.MaxRequestsPerDay))
        {
            throw new InvalidOperationException("Gemini vượt quá giới hạn truy cập.");
        }

        var image = await TryReadImageAsync(imageUrl, cancellationToken);
        if (image == null)
        {
            throw new InvalidOperationException("Tải ảnh thất bại.");
        }

        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = image.Value.MimeType,
                                data = Convert.ToBase64String(image.Value.Bytes)
                            }
                        }
                    }
                }
            }
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/models/{_settings.Model}:generateContent";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini Vision API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Yêu cầu Gemini API thất bại.");
        }

        return ExtractText(body);
    }

    private static bool TryConsumeQuota(DateTime now, string? userKey, int perMinute, int perDay)
    {
        var key = string.IsNullOrWhiteSpace(userKey) ? "anonymous" : userKey.Trim();
        lock (RateLock)
        {
            if (!RequestsPerMinute.TryGetValue(key, out var perMinuteQueue))
            {
                perMinuteQueue = new Queue<DateTime>();
                RequestsPerMinute[key] = perMinuteQueue;
            }

            if (!RequestsPerDay.TryGetValue(key, out var perDayQueue))
            {
                perDayQueue = new Queue<DateTime>();
                RequestsPerDay[key] = perDayQueue;
            }

            while (perMinuteQueue.Count > 0 && (now - perMinuteQueue.Peek()).TotalSeconds >= 60)
            {
                perMinuteQueue.Dequeue();
            }
            while (perDayQueue.Count > 0 && (now - perDayQueue.Peek()).TotalHours >= 24)
            {
                perDayQueue.Dequeue();
            }

            if (perMinuteQueue.Count >= perMinute || perDayQueue.Count >= perDay)
            {
                return false;
            }

            perMinuteQueue.Enqueue(now);
            perDayQueue.Enqueue(now);
            return true;
        }
    }

    private static string ExtractText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var candidates = root.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return string.Empty;
            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            if (parts.GetArrayLength() == 0) return string.Empty;
            return parts[0].GetProperty("text").GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<(byte[] Bytes, string MimeType)?> TryReadImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0 Safari/537.36");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var mimeType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(mimeType) || !mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var contentLength = response.Content.Headers.ContentLength;
            const int maxBytes = 4 * 1024 * 1024;
            if (contentLength.HasValue && contentLength.Value > maxBytes)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                if (memory.Length + read > maxBytes)
                {
                    return null;
                }
                memory.Write(buffer, 0, read);
            }

            return (memory.ToArray(), mimeType);
        }
        catch
        {
            return null;
        }
    }
}

