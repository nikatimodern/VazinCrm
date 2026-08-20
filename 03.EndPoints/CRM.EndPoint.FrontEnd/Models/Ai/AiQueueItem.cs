using System;

namespace CRM.EndPoint.FrontEnd.Models.Ai;

public class AiQueueItem {
    public string CallerNumber { get; set; }
    public Guid? CallerCustomerId { get; set; }
    public string CallerCustomerTxt { get; set; }
    public string DestinationNumber { get; set; }
    public string DestinationUserId { get; set; }
    public string DestinationUserTxt { get; set; }
    public string FilePath { get; set; }
}