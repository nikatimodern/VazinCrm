namespace CRM.EndPoint.FrontEnd.Models.Configuration;

public class SmsIrSetting {
    public string ApiKey { get; set; }
    public long LineNumber { get; set; }
    public SmsPatterns<int> Patterns { get; set; }
}