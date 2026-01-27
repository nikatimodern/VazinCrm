using System;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace CRM.EndPoint.FrontEnd.Services;

public class PushService : IPushService {
    private readonly IConfiguration _config;
    private static PushSubscription _lastSubscription;

    public PushService(IConfiguration config)
    {
        _config = config;
    }
    public string GetPublicKey()
    {
        return _config["Vapid:PublicKey"];
    }
    public async Task SubscribeAsync(PushSubscriptionDto subscription)
    {
        // TODO: ذخیره در DB
        _lastSubscription = new PushSubscription(
            subscription.Endpoint,
            subscription.Keys.P256dh,
            subscription.Keys.Auth
        );
        await Task.CompletedTask;
    }
    public async Task SendAsync(PushSubscription sub, string title, string body, string url)
    {
        var vapidDetails = new VapidDetails(
            "mailto:it@vazinplast.com",
            _config["Vapid:PublicKey"],
            _config["Vapid:PrivateKey"]
        );

        var client = new WebPushClient();

        var payload = JsonConvert.SerializeObject(new
        {
            title,
            body,
            url
        });

        await client.SendNotificationAsync(sub, payload, vapidDetails);
    }

    public async Task SendTestAsync()
    {
        if (_lastSubscription == null)
            throw new Exception("No subscription found");

        await SendAsync(
            _lastSubscription,
            "تست پوش نوتیفیکیشن",
            "چطوری؟",
            "/"
        );
    }
}