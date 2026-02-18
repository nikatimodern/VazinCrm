using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using WebPush;

namespace CRM.EndPoint.FrontEnd.ISerives;

public interface IPushService {
    string GetPublicKey();
    Task SubscribeAsync(PushSubscriptionDto subscription, string userId);
    Task SendToUserAsync(string userId, string title, string body, string url, string tag);
    Task SendToRoleAsync(string roleName, string title, string body, string url, string tag);
    Task SendToAllAsync(string title, string body, string url, string tag);
}