using CRM.Framework.Domain;

namespace CRM.Core.Domain.CallRecordings.Entities;

public class CallRecording : BaseEntity {
    public string CallerNumber { get; set; }
    public string DestinationNumber { get; set; }
    public string RecordingFilePath { get; set; }
    public string TranscriptionText { get; set; }
    public string SummaryText { get; set; }
}