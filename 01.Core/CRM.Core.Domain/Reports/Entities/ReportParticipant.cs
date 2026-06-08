using System;

namespace CRM.Core.Domain.Reports.Entities;

public class ReportParticipant {
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Report Report { get; set; }

    public string Extension { get; set; }
    public string UserId { get; set; }
    public string UserFullName { get; set; }
    public DateTime AnswerTime { get; set; }
    public int SortOrder { get; set; }
}