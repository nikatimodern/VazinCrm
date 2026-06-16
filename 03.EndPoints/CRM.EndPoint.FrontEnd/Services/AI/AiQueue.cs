using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiQueue : IAiQueue {
    private readonly Channel<CallRecording> _queue = Channel.CreateUnbounded<CallRecording>();

    public void Enqueue(CallRecording item)
    {
        _queue.Writer.TryWrite(item);
    }

    public async Task<CallRecording> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}