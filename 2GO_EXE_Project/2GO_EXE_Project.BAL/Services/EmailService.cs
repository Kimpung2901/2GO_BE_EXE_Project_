using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.Interfaces;
using System.Text;
using System.Net.Mail;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class EmailService : IEmailService
{
    private readonly GmailEmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<GmailEmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_settings.AppPassword) && !string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            await SendViaSmtpAsync(to, subject, body, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ClientSecret) ||
            string.IsNullOrWhiteSpace(_settings.RefreshToken))
        {
            _logger.LogWarning("Email not sent: Gmail OAuth settings are missing (ClientId/ClientSecret/RefreshToken).");
            return;
        }

        try
        {
            var credential = await CreateCredentialAsync(cancellationToken);
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _settings.ApplicationName
            });

            var rawMessage = BuildRawMessage(to, subject, body);
            var request = service.Users.Messages.Send(new Message { Raw = rawMessage }, "me");
            var response = await request.ExecuteAsync(cancellationToken);

            if (response is null || string.IsNullOrEmpty(response.Id))
            {
                _logger.LogError("Failed to send email to {To} via Gmail API", to);
                throw new InvalidOperationException("Email sending failed");
            }

            _logger.LogInformation("Sent email to {To} via Gmail API, MessageId: {Id}", to, response.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    private async Task SendViaSmtpAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromEmail
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential(_settings.FromEmail, _settings.AppPassword)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Sent email to {To} via SMTP", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP to {To}", to);
            throw;
        }
    }

    private async Task<UserCredential> CreateCredentialAsync(CancellationToken cancellationToken)
    {
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret
            },
            Scopes = new[] { GmailService.Scope.GmailSend }
        };

        var flow = new GoogleAuthorizationCodeFlow(initializer);
        var token = new TokenResponse
        {
            RefreshToken = _settings.RefreshToken
        };

        var credential = new UserCredential(flow, _settings.UserEmail, token);
        if (credential.Token.IsExpired(SystemClock.Default))
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }

        return credential;
    }

    private string BuildRawMessage(string to, string subject, string body)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromEmail
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        var encodedSubject = Convert.ToBase64String(Encoding.UTF8.GetBytes(subject));
        var mime = new StringBuilder();
        mime.AppendLine("Content-Type: text/html; charset=\"utf-8\"");
        mime.AppendLine($"From: {from}");
        mime.AppendLine($"To: {to}");
        mime.AppendLine($"Subject: =?utf-8?B?{encodedSubject}?=");
        mime.AppendLine();
        mime.AppendLine(body);

        var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime.ToString()))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return raw;
    }
}
