using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices;


public class SmsQueue : ISmsQueue {
    private readonly Channel<SmsJob> _queue = Channel.CreateUnbounded<SmsJob>();

    public async Task QueueAsync(SmsJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<SmsJob> DequeueAsync(CancellationToken token)
    {
        return await _queue.Reader.ReadAsync(token);
    }
}