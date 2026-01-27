using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace CRM.EndPoint.FrontEnd.Services;

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



    public async Task SendToUserAsync(string userId, string title, string body, string url)
    {
        var subs = await db.WebPushSubscriptions
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();

        await SendInternalAsync(subs, title, body, url);
    }

    public async Task SendToRoleAsync(string roleName, string title, string body, string url)
    {
        var usersIds = (await userManager.GetUsersInRoleAsync(roleName)).Select(f => f.Id).ToArray();
        var subs = await db.WebPushSubscriptions
            .Where(x => usersIds.Contains(x.UserId) && x.IsActive)
            .ToListAsync();

        await SendInternalAsync(subs, title, body, url);
    }

    public async Task SendToAllAsync(string title, string body, string url)
    {
        var subs = db.WebPushSubscriptions
            .Where(x => x.IsActive)
            .ToList();

        await SendInternalAsync(subs, title, body, url);
    }

    private async Task SendInternalAsync(IEnumerable<WebPushSubscription> subs, string title, string body, string url)
    {
        foreach (var sub in subs.Where(x => x.IsActive))
        {
            try
            {
                await SendAsync(
                    new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth),
                    title,
                    body,
                    url
                );
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                sub.IsActive = false;
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SendAsync(PushSubscription sub, string title, string body, string url)
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
            url
        });

        await client.SendNotificationAsync(sub, payload, vapidDetails);
    }
}