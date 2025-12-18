namespace _2GO_EXE_Project.BAL.DTOs.Auth;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 30;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
