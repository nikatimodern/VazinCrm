namespace CRM.EndPoint.FrontEnd.Models.PushNotification;

public class PushQueueItem
{
    public string UserId { get; set; }
    public string RoleName { get; set; }
    public bool SendToAll { get; set; }

    public string Title { get; set; }
    public string Body { get; set; }
    public string Url { get; set; }
    public string Tag { get; set; }
}
