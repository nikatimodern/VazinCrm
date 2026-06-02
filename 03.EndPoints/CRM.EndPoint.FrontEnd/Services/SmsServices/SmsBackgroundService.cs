using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices;

public class SmsBackgroundService(
    IServiceScopeFactory scopeFactory,
    ISmsQueue smsQueue,
    IHubContext<TaskHub> hub,
    ILogger<SmsBackgroundService> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await smsQueue.DequeueAsync(stoppingToken);

            await ProcessSms(job, stoppingToken);
        }
    }


    private async Task ProcessSms(SmsJob job, CancellationToken token)
    {
        if (job.Mobile?.Length < 8) return;

        using var scope = scopeFactory.CreateScope();
        var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();
        var notifyUser = !string.IsNullOrEmpty(job.ConnectionId);

        if (notifyUser) await hub.Clients.Client(job.ConnectionId).SendAsync("SmsSending", cancellationToken: token);
        const int maxRetry = 5;

        for (var retry = 1; retry <= maxRetry; retry++)
        {
            try
            {
                var result = await smsService.SendAsync(job);

                if (result.code == 200)
                {
                    logger.LogInformation("Sms sent successfully. Mobile:{Mobile}", job.Mobile);
                    if (notifyUser) await hub.Clients.Client(job.ConnectionId).SendAsync("SmsSuccess", cancellationToken: token);

                    return;
                }

                logger.LogWarning("Sms failed. Retry:{Retry} Mobile:{Mobile} Desc:{Desc}", retry, job.Mobile, result.Desc);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sms Error Retry:{Retry} Mobile:{Mobile}", retry, job.Mobile);
            }

            if (retry < maxRetry)
            {
                await Task.Delay(TimeSpan.FromSeconds(retry), token);
            }
        }

        logger.LogError("Sms failed after all retries. Mobile:{Mobile}", job.Mobile);
        if (notifyUser) await hub.Clients.Client(job.ConnectionId).SendAsync("SmsFailed", cancellationToken: token);
    }
}