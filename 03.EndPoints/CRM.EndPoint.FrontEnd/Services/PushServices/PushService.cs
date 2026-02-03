using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace CRM.EndPoint.FrontEnd.Services.PushServices;

public class PushService : IPushService {
    private readonly IConfiguration _config;
    private readonly CRMContextDb db;
    private readonly UserContextDb udb;
    private readonly UserManager<AppUser> userManager;
    private static PushSubscription _lastSubscription;

    public PushService(IConfiguration config, CRMContextDb db, UserContextDb udb, UserManager<AppUser> userManager)
    {
        _config = config;
        this.db = db;
        this.udb = udb;
        this.userManager = userManager;
    }

    public string GetPublicKey()
    {
        return _config["Vapid:PublicKey"];
    }

    public async Task SubscribeAsync(PushSubscriptionDto subDto, string userId)
    {
        var sub = await db.WebPushSubscriptions.FirstOrDefaultAsync(x => x.Endpoint == subDto.Endpoint);
        if (sub != null)
        {
            sub.P256dh = subDto.Keys.P256dh;
            sub.Auth = subDto.Keys.Auth;
            sub.IsActive = true;
            sub.UserId = userId ?? sub.UserId;
        } else
        {
            sub = new WebPushSubscription {
                Id = Guid.NewGuid(),
                Endpoint = subDto.Endpoint,
                P256dh = subDto.Keys.P256dh,
                Auth = subDto.Keys.Auth,
                UserId = userId,
                CreatedAt = DateTime.Now,
            };
            db.WebPushSubscriptions.Add(sub);
        }

        await db.SaveChangesAsync();
    }


    public async Task SendToUserAsync(string userId, string title, string body, string url, string tag)
    {
        var subs = await db.WebPushSubscriptions
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();

        await SendInternalAsync(subs, title, body, url, tag);
    }

    public async Task SendToRoleAsync(string roleName, string title, string body, string url, string tag)
    {
        var userIds = (await userManager.GetUsersInRoleAsync(roleName)).Select(u => u.Id).ToHashSet();

        var subs = await db.WebPushSubscriptions
            .Where(s => s.IsActive && userIds.Contains(s.UserId))
            .ToListAsync();

        await SendInternalAsync(subs, title, body, url, tag);
    }

    public async Task SendToAllAsync(string title, string body, string url, string tag)
    {
        var subs = db.WebPushSubscriptions
            .Where(x => x.IsActive)
            .ToList();

        await SendInternalAsync(subs, title, body, url, tag);
    }

    private async Task SendInternalAsync(IEnumerable<WebPushSubscription> subs, string title, string body, string url, string tag)
    {
        await Parallel.ForEachAsync(subs, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (sub, ct) => {
            try
            {
                await SendAsync(
                    new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth),
                    title,
                    body,
                    url,
                    tag
                );
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                sub.IsActive = false;
            }
        });


        await db.SaveChangesAsync();
    }

    private async Task SendAsync(PushSubscription sub, string title, string body, string url, string tag = "general")
    {
        var vapidDetails = new VapidDetails(
            "mailto:it@vazinplast.com",
            _config["Vapid:PublicKey"],
            _config["Vapid:PrivateKey"]
        );

        var client = new WebPushClient();

        var payload = JsonConvert.SerializeObject(new {
            title,
            body,
            url,
            tag
        });

        await client.SendNotificationAsync(sub, payload, vapidDetails);
    }
}