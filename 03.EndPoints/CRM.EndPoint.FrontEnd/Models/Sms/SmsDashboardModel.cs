using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CRM.Core.Domain.Sms.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

// SmsDashboard
public sealed class SmsDashboardModel {
    public string Provider { get; set; }
    public decimal? Credit { get; set; }
    public List<string> Lines { get; set; } = [];
}

// SmsReport
public sealed class SmsReportResult {
    public List<SmsReportModel> Data { get; set; } = [];
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class SmsReportModel {
    public long MessageId { get; set; }
    public string Mobile { get; set; }
    public string Number { get; set; }
    public string Type { get; set; }
    public string TypeTxt { get; set; }
    public DateTime? CreatedAtFaraz { get; set; }
    public DateTime SendDateTime { get; set; }
    public string SendDateTimeTxt { get; set; }
    public string DeliveryState { get; set; }
    public string DeliveryStateTxt { get; set; }
    public DateTime? DeliveryDateTime { get; set; }
    public string DeliveryDateTimeTxt { get; set; }
    public string MessageText { get; set; }


    public SmsPatternType? PatternType { get; set; }
    public string PatternTypeTxt { get; set; }
}

// SmsReceive
public sealed class SmsReceiveResult {
    public List<SmsReceiveModel> Data { get; set; } = [];
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class SmsReceiveModel {
    public long MessageId { get; set; }
    public string Mobile { get; set; }
    public string MessageText { get; set; }
    public string Number { get; set; }
    public DateTime? CreatedAtFaraz { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public string ReceivedDateTimeTxt { get; set; }
    public bool Seen { get; set; }
}