using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AiBackgroundService : BackgroundService
{
    private readonly IAiQueue _queue;
    private readonly IServiceProvider _sp;

    public AiBackgroundService(IAiQueue queue, IServiceProvider sp)
    {
        _queue = queue;
        _sp = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var semaphore = new SemaphoreSlim(1); // مثلا 2 تا همزمان

        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await _queue.DequeueAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                await semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var scope = _sp.CreateScope();
                    var aiService = scope.ServiceProvider.GetRequiredService<AiService>();

                    await aiService?.ProcessCallRecording(
                        item.CallerNumber,
                        item.DestinationNumber,
                        item.FilePath
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AI Worker Error: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);
        }
    }
}