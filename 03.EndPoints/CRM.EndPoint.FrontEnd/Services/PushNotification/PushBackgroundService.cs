using System;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Services.PushNotification;

public class PushBackgroundService : BackgroundService {
    private readonly IPushQueue _queue;
    private readonly IPushService _pushService;
    private readonly IServiceScopeFactory scopeFactory;

    public PushBackgroundService(IPushQueue queue, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await _queue.DequeueAsync(stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushService>();
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                var path = item.Url?.Split('?', '#')[0]?.TrimEnd('/') ?? "";
                var entity = path.Equals("/Chat/Main", StringComparison.OrdinalIgnoreCase) ? "Chat" :
                    path.Equals("/Carizo/Index", StringComparison.OrdinalIgnoreCase) ? "Carizo" : null;
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                if (entity != null && configuration.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions"))
                {
                    if (item.SendToAll || !string.IsNullOrEmpty(item.RoleName) || string.IsNullOrEmpty(item.UserId)) continue;
                    var access = scope.ServiceProvider.GetRequiredService<IAccessScopeService>();
                    var grant = await access.GetEffectiveScopeAsync(item.UserId, entity + ".View", cts.Token);
                    if (grant == AccessScopes.None) continue;
                    // Generic notice only; record-specific information is fetched through authorized endpoints.
                    item.Title = entity == "Chat" ? "پیام‌رسان" : "کاریز";
                    item.Body = entity == "Chat" ? "پیام جدیدی دارید." : "فعالیت شما تغییر کرد.";
                    item.Tag = entity;
                }
                if (item.SendToAll)
                {
                    await pushService.SendToAllAsync(item.Title, item.Body, item.Url, item.Tag, cts.Token);
                } else if (!string.IsNullOrEmpty(item.RoleName))
                {
                    await pushService.SendToRoleAsync(item.RoleName, item.Title, item.Body, item.Url, item.Tag, cts.Token);
                } else if (!string.IsNullOrEmpty(item.UserId))
                {
                    await pushService.SendToUserAsync(item.UserId, item.Title, item.Body, item.Url, item.Tag, cts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}