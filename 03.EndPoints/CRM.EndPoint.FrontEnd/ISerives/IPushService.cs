using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using WebPush;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IPushService {
    string GetPublicKey();
    Task SubscribeAsync(PushSubscriptionDto subscription, string userId);
    Task SendToUserAsync(string userId, string title, string body, string url);
    Task SendToRoleAsync(string roleName, string title, string body, string url);
    Task SendToAllAsync(string title, string body, string url);
}