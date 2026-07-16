namespace CRM.Core.Domain.CallRecordings.Enums;

public enum AiProcessStatusEnum {
    Enqueued = 0,

    DownloadingAudio = 10,
    DownloadFailed = 11,

    SendingToAvanegar = 20,
    AvanegarPending = 21,
    TrackingAvanegar = 22,
    AvanegarFailed = 23,

    Summarizing = 30,
    WaitingForRetry = 31,
    SummaryCompleted = 32,
    SummaryFailed = 33,
    SummaryIgnored = 34,

    ReportCreated = 40,
    ReportFailed = 41,

    SystemError = 50,
}