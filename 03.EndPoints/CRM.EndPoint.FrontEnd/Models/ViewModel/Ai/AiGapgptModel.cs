using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

public class SummaryResult
{
    public bool Success { get; set; }
    public SummaryModel Data { get; set; }
    public string Error { get; set; }
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
    public string summary { get; set; }
    public string customer { get; set; }
    public string product { get; set; }
}