using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiPendingWorker(IServiceScopeFactory scopeFactory, ILogger<AiPendingWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Pending AI Worker Started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingCalls(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pending Worker Error");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        logger.LogInformation("Pending AI Worker Stopped");
    }

    private async Task ProcessPendingCalls(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var ai = scope.ServiceProvider.GetRequiredService<AiService>();

        await ai.ProcessPendingCalls(ct);
    }
}