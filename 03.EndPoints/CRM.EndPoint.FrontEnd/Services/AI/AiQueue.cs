using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiQueue : IAiQueue
{
    private readonly Channel<AiQueueItem> _queue = Channel.CreateUnbounded<AiQueueItem>();

    public void Enqueue(AiQueueItem item)
    {
        _queue.Writer.TryWrite(item);
    }

    public async Task<AiQueueItem> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}