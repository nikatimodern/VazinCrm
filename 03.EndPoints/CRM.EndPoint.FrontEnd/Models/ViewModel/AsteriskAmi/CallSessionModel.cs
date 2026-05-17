using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.AsteriskAmi;

public class CallSessionModel {
    public string UniqueId { get; set; }
    public string LinkedId { get; set; }

    public string RecordingPath { get; set; }

    public string Caller { get; set; }
    public string Destination { get; set; }

    public bool Answered { get; set; }

    public DateTime? AnswerTime { get; set; }
}