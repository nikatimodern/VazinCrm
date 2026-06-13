using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Chat;

public class ChatReminderWorker(IServiceProvider serviceProvider, ILogger<ChatReminderWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var reminderService = scope.ServiceProvider.GetRequiredService<ChatReminderService>();

                await reminderService.CheckUnreadChats(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ChatReminderWorker Error");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}