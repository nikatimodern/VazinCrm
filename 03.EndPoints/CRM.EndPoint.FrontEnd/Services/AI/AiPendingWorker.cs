using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Enums;
using CRM.EndPoint.FrontEnd.ISerives;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiPendingWorker(IServiceScopeFactory scopeFactory, ILogger<AiPendingWorker> logger, IAiQueue aiQueue) : BackgroundService {
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
        var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();

        var pendingCalls = await db.CallRecordings
            .Include(x => x.Participants)
            .Where(x => x.AiStatus == AiProcessStatusEnum.AvanegarPending && x.AiAttempts < 3)
            .OrderBy(x => x.CreateDate)
            .Take(10)
            .ToListAsync(ct);

        foreach (var call in pendingCalls)
        {
            call.AiStatus = AiProcessStatusEnum.Enqueued;
            call.AiError = $"Re-enqueued by worker. Previous attempts: {call.AiAttempts}";

            await db.SaveChangesAsync(ct);

            aiQueue?.Enqueue(call);

            logger.LogInformation("Call ID {Id} has been successfully re-enqueued for retry.", call.Id);
        }
    }
}