using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IAiQueue {
    void Enqueue(CallRecording item);
    Task<CallRecording> DequeueAsync(CancellationToken ct);
}