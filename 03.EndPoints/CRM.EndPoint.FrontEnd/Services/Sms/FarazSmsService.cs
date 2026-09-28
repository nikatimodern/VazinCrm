using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Configuration;
using CRM.EndPoint.FrontEnd.Models.Sms;
using FarazSMS;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public class FarazSmsService {
    private readonly HttpClient httpClient;
    private readonly FarazSmsSetting farazConfig;
    private readonly FarazSMS.FarazSMS _farazSms;

    public FarazSmsService(HttpClient httpClient, IConfiguration config)
    {
        this.httpClient = httpClient;
        farazConfig = config.GetSection("FarazSmsConfig").Get<FarazSmsSetting>();
        var apiKey = farazConfig?.ApiKey ?? throw new InvalidOperationException("FarazSMS API Key is not configured.");

        _farazSms = new FarazSMS.FarazSMS(apiKey, httpClient: httpClient);
    }


    public async Task<SmsDashboardModel> GetDashboardAsync()
    {
        var credit = await _farazSms.Balance();
        var lines = await _farazSms.Lines();

        var balance = credit
            .GetProperty("data")
            .GetProperty("balance_amount")
            .GetString();

        var lineList = lines
            .GetProperty("data")
            .EnumerateArray()
            .Select(x => new SmsLineModel {
                Number = x.GetProperty("number").GetString(),
                IsDedicated = x.GetProperty("is_dedicated").GetBoolean(),
                SmsCost = x.GetProperty("sms_cost").GetDecimal()
            })
            .ToList();

        return new SmsDashboardModel {
            Provider = "FarazSMS.ir",
            Credit = decimal.Parse(balance),
            Lines = lineList.Where(f => f.IsDedicated).Select(f => f.Number).ToList()
        };
    }

/*    public async Task<IReadOnlyList<SmsReportModel>> GetReportsAsync(int page = 1, int pageSize = 100)
    {
        var result = await _farazSms.SendRequestItems(page, pageSize);

        if (result.Status != 1 || result.Data == null)
            return [];

        return result.Data
            .Select(x => new SmsReportModel {
                MessageId = x.MessageId,
                MessageText = x.MessageText,
                Mobile = x.Mobile,
                DeliveryState = x.DeliveryState,
                DeliveryDateTime = x.DeliveryDateTime,
                SendDateTime = x.SendDateTime
            })
            .ToList();
    }*/

    public async Task<IReadOnlyList<SmsReceiveModel>> GetReceivesAsync(int page = 1, int pageSize = 100)
    {
        var result = await _farazSms.Inbox(page, pageSize);

        return result
            .GetProperty("data")
            .GetProperty("data")
            .EnumerateArray()
            .Select(x => new SmsReceiveModel {
                Id = x.GetProperty("id").GetInt64(),
                Mobile = x.GetProperty("from").GetString(),
                MessageText = x.GetProperty("message").GetString(),
                Number = x.GetProperty("to").GetString(),
                ReceivedDateTime = x.GetProperty("received_at").GetDateTime(),
                ReceivedDateTimeTxt = x.GetProperty("received_at").GetDateTime().ToString("yyyy/MM/dd HH:mm:ss"),
                Seen = x.GetProperty("seen").GetBoolean()
            })
            .ToList();
    }


    public async Task<ResponseSmsModel> SendPatternAsync(SmsJob job)
    {
        string patternCode;
        Dictionary<string, string> attrs;

        var sms = new FarazSMS.FarazSMS(farazConfig.ApiKey, httpClient: httpClient);

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
                    ["number"] = job.SerialNumber,
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

            // فیدبک مشتری بعد از خرید
            case SmsType.CustomerFeedback:
                patternCode = farazConfig.Patterns.CustomerFeedback;
                attrs = new Dictionary<string, string> {
                    ["number"] = job.SerialNumber
                };
                break;

            default:
                throw new NotSupportedException();
        }

        var res = new ResponseSmsModel();

        if (farazConfig.ActiveStatus != true)
        {
            res.code = 0;
            res.Desc = "ارسال پیامک غیر فعال است";
            return res;
        }

        try
        {
            /*var response = sms.SendPattern(patternCode, job.Mobile, attrs, farazConfig.LineNumber);
            if (response.Status != TaskStatus.Canceled && response.Status != TaskStatus.Faulted)
            {
                res.code = 200;
                res.Desc = response.Result.ToString();
            }*/


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
        catch (FarazException ex)
        {
            res.code = ex.StatusCode;
            res.Desc = ex.Message;
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