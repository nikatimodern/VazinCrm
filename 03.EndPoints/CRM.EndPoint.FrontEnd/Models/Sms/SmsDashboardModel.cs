using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

public sealed class SmsDashboardModel {
    public string Provider { get; set; }
    public decimal? Credit { get; set; }
    public List<string> Lines { get; set; } = [];
    public SmsAccountInfo Account { get; set; }
}

public class SmsLineModel {
    public string Number { get; set; }
    public bool IsDedicated { get; set; }
    public decimal SmsCost { get; set; }
}

public sealed class SmsAccountInfo {
    public string Username { get; set; }
    public string Name { get; set; }
    public string Mobile { get; set; }
}

public sealed class SmsReportModel {
    public long MessageId { get; set; }

    public string Mobile { get; set; }
    public string Number { get; set; }

    public string Type { get; set; }
    public string TypeTxt { get; set; }

    public string DeliveryState { get; set; }
    public string DeliveryStateTxt { get; set; }

    public DateTime SendDateTime { get; set; }
    public string SendDateTimeTxt { get; set; }

    public DateTime? DeliveryDateTime { get; set; }
    public string DeliveryDateTimeTxt { get; set; }

    public string MessageText { get; set; }
}

public sealed class SmsReportResult {
    public List<SmsReportModel> Data { get; set; } = [];
    public int Total { get; set; }
}

public sealed class SmsReceiveResult {
    public List<SmsReceiveModel> Data { get; set; } = [];

    public int Total { get; set; }

    [JsonPropertyName("last_page")] public int TotalPages { get; set; }
}

public class SmsReceiveModel {
    public long Id { get; set; }

    [JsonPropertyName("from")] public string Mobile { get; set; }

    [JsonPropertyName("message")] public string MessageText { get; set; }

    [JsonPropertyName("to")] public string Number { get; set; }

    [JsonPropertyName("created_at")] public DateTime CreatedAtFaraz { get; set; }

    [JsonPropertyName("received_at")] public DateTime ReceivedDateTime { get; set; }

    public string ReceivedDateTimeTxt { get; set; }

    public bool Seen { get; set; }
}