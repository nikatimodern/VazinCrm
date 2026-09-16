using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.Ai;

public class SummaryResult {
    public bool Success { get; init; }
    public SummaryModel Data { get; init; }
    public string Error { get; init; }
    public double? ElapsedSeconds { get; init; }
}

public class AiGapgptModel {
    public List<Choice> Choices { get; init; }
}

public class Choice {
    public Message Message { get; set; }
}

public class Message {
    public string Role { get; set; }
    public string Content { get; set; }
}

public class SummaryModel {
    [JsonPropertyName("summary")] public string Summary { get; init; }

    [JsonPropertyName("customer")] public string Customer { get; init; }

    [JsonPropertyName("product")] public string Product { get; init; }

    [JsonPropertyName("callEvaluation")] public CallEvaluationModel CallEvaluation { get; init; }
}

public class CallEvaluationModel {
    [JsonPropertyName("agentReview")] public string AgentReview { get; set; }

    [JsonPropertyName("customerReview")] public string CustomerReview { get; set; }

    [JsonPropertyName("agentScore")] public int? AgentScore { get; set; }

    [JsonPropertyName("customerScore")] public int? CustomerScore { get; set; }
}