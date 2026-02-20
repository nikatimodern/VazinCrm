using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Services.PushServices;

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