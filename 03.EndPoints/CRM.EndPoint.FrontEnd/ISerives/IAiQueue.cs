using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IAiQueue {
    void Enqueue(AiQueueItem item);
    Task<AiQueueItem> DequeueAsync(CancellationToken ct);
}