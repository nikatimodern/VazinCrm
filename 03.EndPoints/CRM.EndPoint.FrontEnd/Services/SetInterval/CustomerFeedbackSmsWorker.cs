using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.SetInterval;

public sealed class CustomerFeedbackSmsWorker(IServiceScopeFactory scopeFactory, ILogger<CustomerFeedbackSmsWorker> logger) : BackgroundService {
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private const string CustomerFeedbackMessage =
        "مشتری گرامی ضمن قدردانی از خرید شما، خواهشمند است در صورت وجود مغایرت در کیفیت، زمان تحویل یا مقدار محصول دریافتی، عدد ۰ را ارسال فرمایید.با تشکر وزین پلاست";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendFeedbackSmsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while sending customer feedback SMS");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SendFeedbackSmsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();
        var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

        var targetDate = DateTime.Now.AddDays(-7);
        var startDate = new DateTime(2026, 9, 20);

        var factors = await db.Factors.Where(x =>
            x.CustomerFeedbackSmsSentDate == null
            && x.DateMiladi <= targetDate
            && x.DateMiladi >= startDate
        ).ToListAsync(ct);

        if (factors.Count == 0) return;
        
        var smsJob = new SmsJob { Type = SmsType.CustomerFeedback };

        foreach (var factor in factors)
        {
            try
            {
                var mobile = factor.Cus_Mobile;
                if (string.IsNullOrWhiteSpace(mobile)) continue;

//                var result = await smsService.SendTextAsync([mobile], CustomerFeedbackMessage);

                smsJob.Mobile = mobile;
                smsJob.SerialNumber = factor.SerialNumber.ToString();

                var result = await smsService.SendPatternAsync(smsJob);

                if (result?.code != 200) continue;

                factor.CustomerFeedbackSmsSentDate = DateTime.Now;
                await db.SaveChangesAsync(ct);

                logger.LogInformation("feedback SMS sent successfully. Mobile:{Mobile}", mobile);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send feedback SMS for factor {SerialNumber}", factor.SerialNumber);
            }
        }
    }
}