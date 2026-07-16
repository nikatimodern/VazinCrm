using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiBackgroundService(IAiQueue queue, IServiceProvider sp, ILogger<AiBackgroundService> logger) : BackgroundService {
    private const int WorkerCount = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AI workers started: {Count}", WorkerCount);
        var workers = Enumerable.Range(0, WorkerCount)
            .Select(_ => ProcessQueue(stoppingToken));

        await Task.WhenAll(workers);
        logger.LogInformation("AI workers stopped");
    }

    private async Task ProcessQueue(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Waiting queue item");
                var item = await queue.DequeueAsync(stoppingToken);
                logger.LogInformation("Queue item received");

                using var scope = sp.CreateScope();

                var aiService = scope.ServiceProvider.GetRequiredService<AiService>();

                await aiService.ProcessCallRecordingGemini(item, stoppingToken);
                logger.LogInformation("Queue item processed");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Queue stopped");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queue processing failed");
                Console.WriteLine($"Error while processing AI queue: {ex}");
            }
        }
    }
}