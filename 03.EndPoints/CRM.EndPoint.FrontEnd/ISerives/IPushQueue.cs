using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IPushQueue {
    void Enqueue(PushQueueItem item);
    Task<PushQueueItem> DequeueAsync(CancellationToken ct);
}