using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using IPE.SmsIrClient;
using IPE.SmsIrClient.Models.Requests;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public sealed class SmsIrService : ISmsIrService {
    private readonly SmsIr _smsIr;
    private readonly SmsIrSetting _smsConfig;

    public SmsIrService(IConfiguration configuration)
    {
        _smsConfig = configuration?.GetSection("SmsIrConfig").Get<SmsIrSetting>();
        var apiKey = _smsConfig.ApiKey ?? throw new InvalidOperationException("SmsIr API Key is not configured.");

        _smsIr = new SmsIr(apiKey);
    }

    public async Task<SmsDashboardModel> GetDashboardAsync()
    {
        var credit = await _smsIr.GetCreditAsync();
        var lines = await _smsIr.GetLinesAsync();

        return new SmsDashboardModel {
            Provider = "SMS.ir",
            Credit = credit.Status == 1 ? credit.Data : null,
            Lines = lines.Status == 1 ? lines.Data.Select(l => l.ToString()).ToList() : []
        };
    }

    public async Task<IReadOnlyList<SmsReportModel>> GetReportsAsync(int page = 1, int pageSize = 100)
    {
        var result = await _smsIr.GetLiveReportAsync(page, pageSize);

        if (result.Status != 1 || result.Data == null)
            return [];

        return result.Data.Select(x => new SmsReportModel {
            MessageId = x.MessageId,
            Mobile = x.Mobile.ToString(),
            DeliveryState = x.DeliveryState,
            DeliveryStateTxt = GetDeliveryStateText(x.DeliveryState),
            SendDateTime = DateTimeOffset.FromUnixTimeSeconds(x.SendDateTime).DateTime,
            DeliveryDateTime = DateTimeOffset.FromUnixTimeSeconds(x.DeliveryDateTime.Value).DateTime,
            MessageText = x.MessageText
        }).ToList();
    }

    private static string GetDeliveryStateText(byte? state)
    {
        return state switch {
            1 => "تحویل شده",
            2 => "تحویل نشده",
            3 => "در انتظار",
            _ => "نامشخص"
        };
    }

    public async Task<IReadOnlyList<SmsReceiveModel>> GetReceivesAsync(int page = 1, int pageSize = 100, bool sortByNewest = true)
    {
        var result = await _smsIr.GetLiveReceivesAsync(page, pageSize, sortByNewest);

        if (result?.Status != 1 || result.Data == null)
            return [];

        return result.Data
            .Select(x => new SmsReceiveModel {
                Mobile = x.Mobile.ToString(),
                MessageText = x.MessageText,
                Number = x.Number.ToString(),
                ReceivedDateTime = DateTimeOffset.FromUnixTimeSeconds(x.ReceivedDateTime).DateTime
            })
            .ToList();
    }

    public async Task<ResponseSmsModel> SendTextAsync(string[] mobiles, string messageText, int? sendDateTime = null)
    {
        var result = await _smsIr.BulkSendAsync(_smsConfig.LineNumber, messageText, mobiles, sendDateTime);

        return new ResponseSmsModel {
            code = result.Status == 1 ? 200 : result.Status,
            Desc = result.Message
        };
    }


    public async Task<ResponseSmsModel> SendPatternAsync(SmsJob job)
    {
        var res = new ResponseSmsModel();

        try
        {
            int templateId;
            List<VerifySendParameter> parameters;

            switch (job.Type)
            {
                // تایید پیش‌فاکتور توسط مشتری
                case SmsType.AcceptPfactorToCustomer:
                    templateId = _smsConfig.Patterns.AcceptPfactorToCustomer;

                    parameters = [
                        new VerifySendParameter("number", job.SerialNumber),
                        new VerifySendParameter("link", job.PfactorId?.ToString().Substring(0, 7))
                    ];
                    break;

                // تایید پیش‌فاکتور توسط تامین
                case SmsType.AcceptPfactorToTamin:
                    templateId = _smsConfig.Patterns.AcceptPfactorToTamin;

                    parameters = [
                        new VerifySendParameter("number", job.SerialNumber),
                        new VerifySendParameter("link", "?tab=TTaminExpertId")
                    ];
                    break;

                // تایید مدیر فروش برای مشتری
                case SmsType.AcceptedSalesAdminToCustomer:
                    templateId = _smsConfig.Patterns.AcceptedSalesAdminToCustomer;

                    parameters = [new VerifySendParameter("number", job.SerialNumber)];
                    break;

                // محصول آماده تحویل
                case SmsType.ReadyToDeliverToCustomer:
                    templateId = _smsConfig.Patterns.ReadyToDeliverToCustomer;

                    parameters = [new VerifySendParameter("number", job.SerialNumber)];
                    break;

                // لینک واریز پیش‌پرداخت
                case SmsType.PaymentLinkToCustomer:
                    templateId = _smsConfig.Patterns.PaymentPfactorLink;

                    parameters = [
                        new VerifySendParameter("number", job.SerialNumber!),
                        new VerifySendParameter("link", job.PfactorId?.ToString().Substring(0, 7))
                    ];
                    break;

                // OTP Login
                case SmsType.OtpLogin:
                    templateId = _smsConfig.Patterns.OtpLogin;

                    parameters = [new VerifySendParameter("number", job.Code)];
                    break;

                // OTP Forgot Password
                case SmsType.OtpForgot:
                    templateId = _smsConfig.Patterns.OtpForgot;

                    parameters = [new VerifySendParameter("number", job.Code)];
                    break;


                // chat reminder
                case SmsType.ChatReminder:
                    templateId = _smsConfig.Patterns.ChatReminder;

                    parameters = [new VerifySendParameter("number", job.Mobile)];
                    break;


                default:
                    throw new NotSupportedException($"SMS type '{job.Type}' is not supported by SMS.ir.");
            }

            var result = await _smsIr.VerifySendAsync(job.Mobile, templateId, parameters.ToArray());

            res.code = result.Status == 1 ? 200 : result.Status;
            res.Desc = result.Message;

            return res;
        }
        catch (Exception ex)
        {
            res.code = 0;
            res.Desc = ex.Message;

            return res;
        }
    }
}