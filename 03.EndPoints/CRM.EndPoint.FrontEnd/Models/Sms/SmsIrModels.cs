using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

public sealed class SmsIrParameter {
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
}

public sealed class SmsIrSendBulkRequest {
    [JsonPropertyName("lineNumber")] public long LineNumber { get; set; }

    [JsonPropertyName("MessageText")] public string MessageText { get; set; } = string.Empty;

    [JsonPropertyName("Mobiles")] public List<string> Mobiles { get; set; } = [];

    [JsonPropertyName("SendDateTime")] public long? SendDateTime { get; set; }
}

public sealed class SmsIrSendLikeToLikeRequest {
    [JsonPropertyName("lineNumber")] public long LineNumber { get; set; }

    [JsonPropertyName("MessageTexts")] public List<string> MessageTexts { get; set; } = [];

    [JsonPropertyName("Mobiles")] public List<string> Mobiles { get; set; } = [];

    [JsonPropertyName("SendDateTime")] public long? SendDateTime { get; set; }
}

public sealed class SmsIrVerifyRequest {
    [JsonPropertyName("Mobile")] public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("TemplateId")] public int TemplateId { get; set; }

    [JsonPropertyName("Parameters")] public List<SmsIrParameter> Parameters { get; set; } = [];
}

public sealed class SmsIrApiResponse<T> {
    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public T Data { get; set; }
}

public sealed class SmsIrSendResponse {
    [JsonPropertyName("messageId")] public long MessageId { get; set; }

    [JsonPropertyName("cost")] public decimal Cost { get; set; }

    public Guid? PackId { get; set; }
}

public sealed class SmsIrBulkSendResponse {
    [JsonPropertyName("packId")] public Guid PackId { get; set; }

    [JsonPropertyName("messageIds")] public List<long> MessageIds { get; set; } = [];

    [JsonPropertyName("cost")] public decimal Cost { get; set; }
}

public sealed class SmsIrCreditResponse {
    [JsonPropertyName("credit")] public decimal Credit { get; set; }
}

public sealed class SmsIrLine {
    [JsonPropertyName("number")] public long Number { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }
}

public sealed class SmsIrApiException : Exception {
    public int StatusCode { get; }
    public string ApiMessage { get; }

    public SmsIrApiException(int statusCode, string apiMessage) : base(apiMessage ?? $"SMS.ir request failed with status {statusCode}.")
    {
        StatusCode = statusCode;
        ApiMessage = apiMessage;
    }
}

public sealed class SmsIrSendBulkResponse {
    [JsonPropertyName("packId")] public Guid PackId { get; set; }

    [JsonPropertyName("messageIds")] public List<long> MessageIds { get; set; } = [];

    [JsonPropertyName("cost")] public decimal Cost { get; set; }
}