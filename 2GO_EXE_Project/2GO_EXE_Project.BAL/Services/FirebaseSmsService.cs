using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class FirebaseSmsService : ISmsService
{
    private readonly FirebaseSmsSettings _settings;
    private readonly ILogger<FirebaseSmsService> _logger;
    private readonly HttpClient _httpClient;

    public FirebaseSmsService(IOptions<FirebaseSmsSettings> options, ILogger<FirebaseSmsService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.FunctionUrl))
        {
            throw new InvalidOperationException("Firebase SMS FunctionUrl is not configured.");
        }

        var payload = new
        {
            phone,
            message,
            apiKey = _settings.ApiKey
        };

        var response = await _httpClient.PostAsJsonAsync(_settings.FunctionUrl, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send SMS to {Phone}. Status: {Status}. Body: {Body}", phone, response.StatusCode, body);
            throw new InvalidOperationException("Failed to send SMS.");
        }

        _logger.LogInformation("Sent SMS to {Phone}", phone);
    }
}
