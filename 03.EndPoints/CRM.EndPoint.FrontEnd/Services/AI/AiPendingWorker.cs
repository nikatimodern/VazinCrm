using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Enums;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
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
        var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

        var calls = await db.CallRecordings
            .Include(x => x.Participants)
            .Where(x => x.AiStatus == AiProcessStatusEnum.AvanegarPending)
            .OrderBy(x => x.CreateDate)
            .Take(10)
            .ToListAsync(ct);

        foreach (var call in calls)
        {
            await ai.ProcessPendingCall(call, ct);
        }
    }
}