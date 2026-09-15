namespace CRM.EndPoint.FrontEnd.Services.Sms;

public sealed class SmsIrOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public long LineNumber { get; set; }
    public string ApiUrl { get; set; } = "https://api.sms.ir/v1";
    public int TimeoutSeconds { get; set; } = 30;
}