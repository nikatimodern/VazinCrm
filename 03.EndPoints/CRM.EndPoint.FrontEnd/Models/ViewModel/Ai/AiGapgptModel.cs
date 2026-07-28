using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

public class SummaryResult {
    public bool Success { get; set; }
    public SummaryModel Data { get; set; }
    public string Error { get; set; }
    public double? ElapsedSeconds { get; set; }
}

public class AiGapgptModel {
    public List<Choice> Choices { get; set; }
}

public class Choice {
    public Message Message { get; set; }
}

public class Message {
    public string Role { get; set; }
    public string Content { get; set; }
}

public class SummaryModel {
    [JsonPropertyName("summary")] public string Summary { get; set; }

    [JsonPropertyName("customer")] public string Customer { get; set; }

    [JsonPropertyName("product")] public string Product { get; set; }

    [JsonPropertyName("salesAgent")] public SalesAgentModel SalesAgent { get; set; }
}

public class SalesAgentModel {
    [JsonPropertyName("review")] public string Review { get; set; }

    [JsonPropertyName("score")] public int? Score { get; set; }

    [JsonPropertyName("customerScore")] public int? CustomerScore { get; set; }
}