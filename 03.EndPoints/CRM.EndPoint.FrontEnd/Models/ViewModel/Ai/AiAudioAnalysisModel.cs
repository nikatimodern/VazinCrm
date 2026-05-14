using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

public class AiAudioAnalysisModel {
    [JsonPropertyName("call_info")] public CallInfo CallInfo { get; set; }

    [JsonPropertyName("speakers")] public Speakers Speakers { get; set; }

    [JsonPropertyName("caller_analysis")] public CallerAnalysis CallerAnalysis { get; set; }

    [JsonPropertyName("agent_analysis")] public AgentAnalysis AgentAnalysis { get; set; }
}

public class CallInfo {
    [JsonPropertyName("category")] public string Category { get; set; }

    [JsonPropertyName("summary")] public string Summary { get; set; }
}

public class Speakers {
    [JsonPropertyName("Speaker A")] public string SpeakerA { get; set; }

    [JsonPropertyName("Speaker B")] public string SpeakerB { get; set; }
}

public class CallerAnalysis {
    [JsonPropertyName("needs_and_intent")] public string NeedsAndIntent { get; set; }

    [JsonPropertyName("pain_points")] public string PainPoints { get; set; }

    [JsonPropertyName("sentiment")] public string Sentiment { get; set; }

    [JsonPropertyName("requested_products")]
    public List<string> RequestedProducts { get; set; } = [];
}

public class AgentAnalysis {
    [JsonPropertyName("performance_summary")]
    public string PerformanceSummary { get; set; }

    [JsonPropertyName("professionalism_evaluation")]
    public string ProfessionalismEvaluation { get; set; }

    [JsonPropertyName("strengths")] public List<string> Strengths { get; set; } = [];

    [JsonPropertyName("weaknesses")] public List<string> Weaknesses { get; set; } = [];

    [JsonPropertyName("score")] public int Score { get; set; }
}