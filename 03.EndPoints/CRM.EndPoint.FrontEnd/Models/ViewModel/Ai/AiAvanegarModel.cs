using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

public class AiAvanegarModel {
    [JsonPropertyName("data")] public DataAvanegar Data { get; set; }

    [JsonPropertyName("meta")] public MetaRootAvanegar Meta { get; set; }
}

public class DataAvanegar {
    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public InnerDataAvanegar Data { get; set; }
}

public class MetaRootAvanegar {
    [JsonPropertyName("shamsiDate")] public string ShamsiDate { get; set; }

    [JsonPropertyName("requestId")] public string RequestId { get; set; }
}

public class InnerDataAvanegar {
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("traceId")] public string TraceId { get; set; }

    [JsonPropertyName("estimationTime")] public int? EstimationTime { get; set; }

    [JsonPropertyName("aiResponse")] public AiResponseDetails AiResponse { get; set; }
}

public class AiResponseDetails {
    [JsonPropertyName("requestId")] public string RequestId { get; set; }

    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("result")] public AiResult Result { get; set; }
}

public class AiResult {
    [JsonPropertyName("text")] public string Text { get; set; }
}