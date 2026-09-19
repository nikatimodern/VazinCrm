using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

public sealed class SmsDashboardModel
{
    public string Provider { get; set; }
    public decimal? Credit { get; set; }
    public List<long> Lines { get; set; } = [];
    public SmsAccountInfo Account { get; set; }
}

public sealed class SmsAccountInfo
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Mobile { get; set; }
}

public sealed class SmsReportModel
{
    public long MessageId { get; set; }
    public long Mobile { get; set; }
    public byte? DeliveryState { get; set; }
    public int SendDateTime { get; set; }
    public int? DeliveryDateTime { get; set; }
    public string MessageText { get; set; }
}

public sealed class SmsReceiveModel
{
    public long Mobile { get; set; }
    public string MessageText { get; set; }
    public long Number { get; set; }
    public int ReceivedDateTime { get; set; }
}