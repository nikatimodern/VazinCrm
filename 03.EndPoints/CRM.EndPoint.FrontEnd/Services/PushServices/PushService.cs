using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using CRM.Infrastructures.Sql;
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
    private readonly UserManager<AppUser> userManager;
    private readonly WebPushClient client;
    private readonly VapidDetails _vapidDetails;
    public PushService(IConfiguration config, CRMContextDb db, UserManager<AppUser> userManager, WebPushClient client)
    {
        _config = config;
        this.db = db;
        this.userManager = userManager;
        this.client = client;
        _vapidDetails = new VapidDetails(
            "mailto:it@vazinplast.com",
            config?["Vapid:PublicKey"],
            config?["Vapid:PrivateKey"]
        );
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


    public async Task SendToUserAsync(string userId, string title, string body, string url, string tag, CancellationToken ctsToken)
    {
        var subs = await db.WebPushSubscriptions
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken: ctsToken);

        await SendInternalAsync(subs, title, body, url, tag, ctsToken);
    }

    public async Task SendToRoleAsync(string roleName, string title, string body, string url, string tag, CancellationToken ctsToken)
    {
        var userIds = (await userManager.GetUsersInRoleAsync(roleName)).Select(u => u.Id).ToHashSet();

        var subs = await db.WebPushSubscriptions
            .Where(s => s.IsActive && userIds.Contains(s.UserId))
            .ToListAsync(cancellationToken: ctsToken);

        await SendInternalAsync(subs, title, body, url, tag, ctsToken);
    }

    public async Task SendToAllAsync(string title, string body, string url, string tag, CancellationToken ctsToken)
    {
        var subs = await db.WebPushSubscriptions
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken: ctsToken);

        await SendInternalAsync(subs, title, body, url, tag, ctsToken);
    }

    private async Task SendInternalAsync(IEnumerable<WebPushSubscription> subs, string title, string body, string url, string tag,
        CancellationToken ctsToken)
    {
        foreach (var sub in subs.Where(x => x.IsActive))
        {
            try
            {
                await SendAsync(
                    new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth),
                    title,
                    body,
                    url,
                    tag,
                    ctsToken
                );
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                sub.IsActive = false;
            }

            await Task.Delay(50, ctsToken);
        }

        await db.SaveChangesAsync(ctsToken);
    }

    private async Task SendAsync(PushSubscription sub, string title, string body, string url, string tag, CancellationToken ctsToken)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromMilliseconds(500);

        var payload = JsonConvert.SerializeObject(new {
            title,
            body,
            url,
            tag = string.IsNullOrEmpty(tag) ? "general" : tag,
        });

        var options = new Dictionary<string, object>
        {
            ["vapidDetails"] = _vapidDetails,
            ["TTL"] = 300,
        };

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await client.SendNotificationAsync(sub, payload, options, ctsToken);
                return;
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                Console.WriteLine($"Attempt {attempt} failed, retrying...");

                await Task.Delay(delay, ctsToken);
                delay *= 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /*try
        {
            var vapidDetails = new VapidDetails(
                "mailto:it@vazinplast.com",
                _config["Vapid:PublicKey"],
                _config["Vapid:PrivateKey"]
            );

            var payload = JsonConvert.SerializeObject(new {
                title,
                body,
                url,
                tag
            });

            await client.SendNotificationAsync(sub, payload, vapidDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }*/
    }
}