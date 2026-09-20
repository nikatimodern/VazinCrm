using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public class FarazSmsService {
    private readonly HttpClient httpClient;
    private readonly FarazSmsSetting farazConfig;

    public FarazSmsService(HttpClient httpClient, IConfiguration config)
    {
        this.httpClient = httpClient;
        farazConfig = config.GetSection("FarazSmsConfig").Get<FarazSmsSetting>();
    }

    public async Task<ResponseSmsModel> SendPatternAsync(SmsJob job)
    {
        ResponseSmsModel res = new();

        var patternCode = string.Empty;
        Dictionary<string, string> attrs = null;

        switch (job.Type)
        {
            // تایید پیش‌فاکتور توسط مشتری
            case SmsType.AcceptPfactorToCustomer:
                patternCode = farazConfig.Patterns.AcceptPfactorToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // تایید پیش‌فاکتور توسط تامین
            case SmsType.AcceptPfactorToTamin:
                patternCode = farazConfig.Patterns.AcceptPfactorToTamin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber,
                    ["link"] = "Cartable/Index?tab=TTaminExpertId"
                };
                break;

            // تایید مدیر فروش (برای مشتری)
            case SmsType.AcceptedSalesAdminToCustomer:
                patternCode = farazConfig.Patterns.AcceptedSalesAdminToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // محصول آماده تحویل (برای مشتری)
            case SmsType.ReadyToDeliverToCustomer:
                patternCode = farazConfig.Patterns.ReadyToDeliverToCustomer;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            // لینک واریز پیش پرداخت (برای مشتری)
            case SmsType.PaymentLinkToCustomer:
                patternCode = farazConfig.Patterns.PaymentPfactorLink;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber!,
                    ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                };
                break;

            // ارسال OTP
            case SmsType.OtpLogin:
                patternCode = farazConfig.Patterns.OtpLogin;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            case SmsType.OtpForgot:
                patternCode = farazConfig.Patterns.OtpForgot;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Code
                };
                break;

            // chat reminder
            case SmsType.ChatReminder:
                patternCode = farazConfig.Patterns.ChatReminder;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.Mobile
                };
                break;

            default:
                throw new NotSupportedException();
        }


        if (farazConfig.ActiveStatus != true)
        {
            res.code = 0;
            res.Desc = "ارسال پیامک غیر فعال است";
            return res;
        }

        try
        {
            var model = new FarazSmsPatternRequest {
                code = patternCode,
                recipient = job.Mobile,
                line_number = farazConfig.LineNumber,
                attributes = attrs
            };


            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("pattern", content);

            var responseText = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<FarazSmsResponse>(responseText);

            res.code = response.IsSuccessStatusCode ? 200 : 0;
            res.Desc = !string.IsNullOrEmpty(result?.message) ? result.message : result?.status ?? responseText;

            return res;
        }
        catch (Exception ex)
        {
            res.code = 0;
            res.Desc = ex.Message;
            return res;
        }
    }


    public async Task<ResponseSmsModel> SendTextAsync(string[] mobiles, string message)
    {
        var res = new ResponseSmsModel();

        if (farazConfig.ActiveStatus != true)
        {
            res.code = 0;
            res.Desc = "ارسال پیامک غیر فعال است";
            return res;
        }

        try
        {
            var model = new FarazSmsTextRequest {
                recipients = mobiles,
                text = message,
                line_number = farazConfig.LineNumber
            };

            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("simple", content);
            var responseText = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<FarazSmsResponse>(responseText);

            res.code = response.IsSuccessStatusCode ? 200 : 0;
            res.Desc = !string.IsNullOrEmpty(result?.message) ? result.message : result?.status ?? responseText;

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