namespace _2GO_EXE_Project.BAL.Settings;

public class FirebaseSmsSettings
{
    public string FunctionUrl { get; set; } = string.Empty; // Cloud Function HTTPS endpoint to send SMS
    public string ApiKey { get; set; } = string.Empty; // Optional auth key for your function
}
