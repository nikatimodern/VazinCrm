using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public class SmsSyncWorker(IServiceScopeFactory scopeFactory, ILogger<SmsSyncWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                var nextRun = now.Date.AddDays(1).AddHours(2); // هر روز ساعت ۲ بامداد

                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                using var scope = scopeFactory.CreateScope();

                var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

                await smsService.SyncReceiveSmsAsync(stoppingToken); // سینک پیامهای دریافتی

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                await smsService.SyncSentSmsAsync(stoppingToken); // سینک پیام های ارسالی
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "خطا در Sync پیام‌های دریافتی SMS");
            }
        }
    }
}