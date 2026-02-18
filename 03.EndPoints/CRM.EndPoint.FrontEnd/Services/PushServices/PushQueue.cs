using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;

namespace CRM.EndPoint.FrontEnd.Services.PushServices;

using System.Threading.Channels;

public class PushQueue : IPushQueue
{
    private readonly Channel<PushQueueItem> _queue = Channel.CreateUnbounded<PushQueueItem>();

    public void Enqueue(PushQueueItem item)
    {
        _queue.Writer.TryWrite(item);
    }

    public async Task<PushQueueItem> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
