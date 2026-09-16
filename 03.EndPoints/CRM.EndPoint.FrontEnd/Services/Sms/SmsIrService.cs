using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using IPE.SmsIrClient;
using IPE.SmsIrClient.Models.Requests;
using IPE.SmsIrClient.Models.Results;
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

    public Task<SmsIrResult<SendResult>> SendAsync(string mobile, string messageText)
    {
        return _smsIr.BulkSendAsync(_smsConfig.LineNumber, messageText, [mobile]);
    }

    public Task<SmsIrResult<SendResult>> BulkSendAsync(string messageText, string[] mobiles, int? sendDateTime = null)
    {
        return _smsIr.BulkSendAsync(_smsConfig.LineNumber, messageText, mobiles, sendDateTime);
    }


    public async Task<ResponseSmsModel> SendPatternSms(SmsJob job)
    {
        var res = new ResponseSmsModel();

        try
        {
            var templateId = 0;
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
                        new VerifySendParameter("link", "Cartable/Index?tab=TTaminExpertId")
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

            res.code = result.Status;
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