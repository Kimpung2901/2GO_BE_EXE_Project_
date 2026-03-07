namespace _2GO_EXE_Project.BAL.Settings;

public class GmailEmailSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "2GO_EXE_Project";
    public string AppPassword { get; set; } = string.Empty;
}
