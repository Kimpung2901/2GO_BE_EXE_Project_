using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // TODO: plug real provider (SMTP/SendGrid). For now we log for traceability.
        _logger.LogInformation("Sending email to {To}. Subject: {Subject}. Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
