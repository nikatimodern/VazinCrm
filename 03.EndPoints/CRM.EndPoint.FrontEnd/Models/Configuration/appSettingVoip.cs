namespace CRM.EndPoint.FrontEnd.Models.Configuration;

public class AppSettingVoip {
    public string _hostname { get; set; }
    public int _port { get; set; }
    public string _username { get; set; }
    public string _password { get; set; }
    public bool? ActiveStatus { get; set; }
}