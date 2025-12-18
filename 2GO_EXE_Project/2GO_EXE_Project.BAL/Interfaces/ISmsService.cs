namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}
