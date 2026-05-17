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

public class AiBackgroundService : BackgroundService {
    private readonly IAiQueue _queue;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AiBackgroundService> logger;

    private const int WorkerCount = 2;

    public AiBackgroundService(IAiQueue queue, IServiceProvider sp, ILogger<AiBackgroundService> logger)
    {
        _queue = queue;
        _sp = sp;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, WorkerCount)
            .Select(_ => ProcessQueue(stoppingToken));

        await Task.WhenAll(workers);
    }

    private async Task ProcessQueue(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _queue.DequeueAsync(stoppingToken);

                using var scope = _sp.CreateScope();

                var aiService = scope.ServiceProvider.GetRequiredService<AiService>();

                await aiService.ProcessCallRecording(item);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while processing AI queue: {ex.Message}");
                Console.WriteLine($"Error while processing AI queue: {ex}");
            }
        }
    }
}