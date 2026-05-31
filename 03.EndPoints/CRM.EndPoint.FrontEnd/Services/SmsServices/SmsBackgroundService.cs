using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices;

public class SmsBackgroundService(IServiceScopeFactory scopeFactory, ISmsQueue smsQueue, IHubContext<TaskHub> hub) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await smsQueue.DequeueAsync(stoppingToken);

            await ProcessSms(job);
        }
    }

    private async Task ProcessSms(SmsJob job)
    {
        using var scope = scopeFactory.CreateScope();
        var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

        const int maxRetry = 3;

        for (var retry = 1; retry <= maxRetry; retry++)
        {
            try
            {
                var result = await smsService.SendAsync(job);

                if (result.code == 200)
                {
                    await hub.Clients.User(job.UserId).SendAsync("SmsSent");
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(retry));
        }
    }

}