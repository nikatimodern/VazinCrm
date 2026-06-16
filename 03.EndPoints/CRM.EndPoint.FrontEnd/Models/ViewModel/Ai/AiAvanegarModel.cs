using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

public class AiAvanegarModel {
    [JsonPropertyName("data")] public DataContainer Data { get; set; }

    [JsonPropertyName("meta")] public MetaRoot Meta { get; set; }
}

public class DataContainer {
    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public InnerData Data { get; set; }
}

public class InnerData {
    [JsonPropertyName("aiResponse")] public AiResponseDetails AiResponse { get; set; }
}

public class AiResponseDetails {
    [JsonPropertyName("result")] public AiResult Result { get; set; }
}

public class AiResult {
    [JsonPropertyName("text")] public string Text { get; set; }
}

public class MetaRoot {
    [JsonPropertyName("shamsiDate")] public string ShamsiDate { get; set; }
}