using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.CallRecordings.Enums;
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
    public string Product { get; set; }
    public int DurationSeconds { get; set; }

    public List<ReportParticipant> CallParticipant { get; set; } = [];
    public Report Report { get; set; }

    private AiProcessStatusEnum _AiProcessStatus = AiProcessStatusEnum.Pending;
    public AiProcessStatusEnum AiStatus {
        get => _AiProcessStatus;
        set {
            _AiProcessStatus = value;
            AiStatusTxt = _AiProcessStatus.ToString();
        }
    }
    public string AiStatusTxt { get; private set; }

    public string AiError { get; set; }
    public string AvanegarRequestId { get; set; }
    public int AvanegarAttempts { get; set; }
    public DateTime? LastTrackDate { get; set; }
    public DateTime? NextTrackDate { get; set; }
}
