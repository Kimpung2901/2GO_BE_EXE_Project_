namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateAsync(string prompt, string? userKey = null, CancellationToken cancellationToken = default);
    Task<string> GenerateFromImageAsync(string prompt, string imageUrl, string? userKey = null, CancellationToken cancellationToken = default);
}
