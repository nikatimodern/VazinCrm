using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using WebPush;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IPushService {
    string GetPublicKey();
    Task SubscribeAsync(PushSubscriptionDto subscription);
    Task SendAsync(PushSubscription subscription, string title, string body, string url);
    Task SendTestAsync();
}