using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.PushNotification;

namespace CRM.EndPoint.FrontEnd.IServices.PushNotification;

public interface IPushQueue {
    void Enqueue(PushQueueItem item);
    Task<PushQueueItem> DequeueAsync(CancellationToken ct);
}