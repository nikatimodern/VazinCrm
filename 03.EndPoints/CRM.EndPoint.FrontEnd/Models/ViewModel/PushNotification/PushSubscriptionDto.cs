namespace CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;

public class PushSubscriptionDto
{
    public string Endpoint { get; set; }
    public PushKeys Keys { get; set; }
}

public class PushKeys
{
    public string P256dh { get; set; }
    public string Auth { get; set; }
}
