using System;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.CallRecordings.Entities;

namespace CRM.Core.Domain.Reports.Entities;

public class ReportParticipant {
    public Guid Id { get; set; }
    public Guid? ReportId { get; set; }
    public Report Report { get; set; }
    public Guid? CallRecordingId { get; set; }
    public CallRecording CallRecording { get; set; }

    public string Extension { get; set; }
    public string UserId { get; set; }
    public string UserFullName { get; set; }
    public DateTime AnswerTime { get; set; }
    public string AnswerTimeTxt { get; set; }
    public int SortOrder { get; set; }

    [NotMapped] public string UserImage { get; set; }
}