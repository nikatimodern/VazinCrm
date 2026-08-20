using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.PushNotification;

namespace CRM.EndPoint.FrontEnd.IServices.PushNotification;

public interface IPushService {
    string GetPublicKey();
    Task SubscribeAsync(PushSubscriptionDto subscription, string userId);
    Task SendToUserAsync(string userId, string title, string body, string url, string tag, CancellationToken ctsToken);
    Task SendToRoleAsync(string roleName, string title, string body, string url, string tag, CancellationToken ctsToken);
    Task SendToAllAsync(string title, string body, string url, string tag, CancellationToken ctsToken);
}