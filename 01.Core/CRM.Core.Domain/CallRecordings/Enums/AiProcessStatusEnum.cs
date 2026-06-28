namespace CRM.Core.Domain.CallRecordings.Enums;

public enum AiProcessStatusEnum {
    Pending = 0,

    DownloadingAudio = 10,
    DownloadFailed = 11,

    SendingToAvanegar = 20,
    AvanegarPending = 21,
    TrackingAvanegar = 22,
    AvanegarFailed = 23,

    Summarizing = 30,
    SummaryFailed = 31,
    SummaryIgnored = 32,
    SummaryCompleted = 33,

    ReportCreated = 40,
    ReportFailed = 41,

    SystemError = 50
}