using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices;

public interface ISmsQueue {
    Task QueueAsync(SmsJob job);

    ValueTask<SmsJob> DequeueAsync(CancellationToken token);
}