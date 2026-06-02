using System;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Reports.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.CallRecordings.Entities;

public class CallRecording : BaseEntity {
    public string CallerNumber { get; set; }
    public Guid? CallerCustomerId { get; set; }
    public string CallerCustomerTxt { get; set; }
    [NotMapped] public int? CallerCustomerCode { get; set; }
    public string DestinationNumber { get; set; }
    public string DestinationUserId { get; set; }
    public string DestinationUserTxt { get; set; }
    public string RecordingFilePath { get; set; }
    public string TranscriptionText { get; set; }
    public string SummaryText { get; set; }

    public Report Report { get; set; }
}