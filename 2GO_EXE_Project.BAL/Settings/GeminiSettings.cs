namespace _2GO_EXE_Project.BAL.Settings;

public class GeminiSettings
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public int MaxRequestsPerMinute { get; set; } = 15;
    public int MaxRequestsPerDay { get; set; } = 200;
}
