using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.SystemWarnings.Enums;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.SetInterval;

public sealed class CustomerFeedbackSmsWorker(IServiceScopeFactory scopeFactory, ILogger<CustomerFeedbackSmsWorker> logger) : BackgroundService {
    private const string CustomerFeedbackMessage =
        "مشتری گرامی ضمن قدردانی از خرید شما، خواهشمند است در صورت وجود مغایرت در کیفیت، زمان تحویل یا مقدار محصول دریافتی، عدد ۰ را ارسال فرمایید.با تشکر وزین پلاست";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(9); // فردا ساعت 09:00

                if (now >= nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                logger.LogInformation("Customer feedback SMS worker will run at {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                await SendFeedbackSmsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while sending customer feedback SMS");
            }
        }
    }


    private async Task SendFeedbackSmsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CRMContextDb>();
        var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();
        var service = scope.ServiceProvider.GetRequiredService<Service>();

        var startDate = new DateTime(2026, 8, 29);
        var targetDate = DateTime.Now.AddDays(-7);

        var factors = await db.Factors
            .Where(x =>
                x.CustomerFeedbackSmsSentDate == null &&
                x.DateMiladi >= startDate &&
                x.DateMiladi <= targetDate &&
                x.status != FactorStatus.ابطال_شده
            )
            .OrderBy(x => x.DateMiladi)
            .ToListAsync(ct);

        if (factors.Count == 0)
        {
            logger.LogInformation("No customer feedback SMS found");

            return;
        }

        logger.LogInformation("Found {Count} factors for customer feedback SMS", factors.Count);

        var smsJob = new SmsJob { Type = SmsType.CustomerFeedback };

        var requestCount = 0;

        foreach (var factor in factors)
        {
            ct.ThrowIfCancellationRequested(); // وقتی iis استاپ بشه اجرا میشه و میفته توی catch

            var mobile = TextService.NormalizeIranMobile(factor.Cus_Mobile);

            if (!TextService.IsValidPhoneNumber(mobile))
            {
                logger.LogWarning("MobileNumber is not valid. Mobile:{Mobile}, customerCode: {customerCode}", mobile, factor.CustomerCode);
                await service.AddWarning(factor.Id, EntityTypeEnum.Factor, WarningTypeEnum.عدم_ارسال_پیامک_کنترل_کیفیت);
                await db.SaveChangesAsync(ct);
                continue;
            }

            try
            {
                smsJob.Mobile = mobile;
                smsJob.SerialNumber = factor.SerialNumber.ToString();

                var result = await smsService.SendPatternAsync(smsJob);

                requestCount++;

                if (result?.code != 200)
                {
                    logger.LogWarning("Feedback SMS failed. SerialNumber:{SerialNumber}, Mobile:{Mobile}", factor.SerialNumber, mobile);
                    await service.AddWarning(factor.Id, EntityTypeEnum.Factor, WarningTypeEnum.عدم_ارسال_پیامک_کنترل_کیفیت);
                    await db.SaveChangesAsync(ct);
                    continue;
                }

                factor.CustomerFeedbackSmsSentDate = DateTime.Now;

                await db.SaveChangesAsync(ct);

                logger.LogInformation("Feedback SMS sent successfully. SerialNumber:{SerialNumber}, Mobile:{Mobile}", factor.SerialNumber, mobile);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send feedback SMS for factor {SerialNumber}, Mobile:{Mobile}", factor.SerialNumber, mobile);
                await service.AddWarning(factor.Id, EntityTypeEnum.Factor, WarningTypeEnum.عدم_ارسال_پیامک_کنترل_کیفیت);
                await db.SaveChangesAsync(ct);
            }

            // Provider limit: 50 requests per minute
            if (requestCount >= 50)
            {
                logger.LogInformation("SMS provider rate limit reached. Waiting 100 seconds...");

                await Task.Delay(TimeSpan.FromSeconds(100), ct);
                requestCount = 0;
            }
        }
    }


/*
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

//        return;
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
*/
}