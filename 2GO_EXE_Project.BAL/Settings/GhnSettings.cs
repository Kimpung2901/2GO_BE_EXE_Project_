namespace _2GO_EXE_Project.BAL.Settings;

public class GhnSettings
{
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";
    public string Token { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public string WebhookToken { get; set; } = string.Empty;
}
