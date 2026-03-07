namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
