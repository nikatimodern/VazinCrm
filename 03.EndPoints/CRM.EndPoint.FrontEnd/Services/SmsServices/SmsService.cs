using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices {
    public class SmsService {
        private readonly RequestServices request;
        private readonly HttpClient httpClient;
        private readonly appSettingSms smsConfig;

        public SmsService(RequestServices request, HttpClient httpClient, IConfiguration config)
        {
            this.request = request;
            this.httpClient = httpClient;
            smsConfig = config.GetSection("smsConfig").Get<appSettingSms>();
        }

        private async Task<ResponseSmsModel> SendPatternSms(string toNumber, string patternCode, Dictionary<string, string> attrs)
        {
            ResponseSmsModel res = new();

            if (smsConfig.ActiveStatus != true)
            {
                res.code = 0;
                res.Desc = "ارسال پیامک غیر فعال است";
                return res;
            }

            try
            {
                var model = new PatternSmsRequest {
                    code = patternCode,
                    recipient = toNumber,
                    line_number = smsConfig._fromNumber,
                    attributes = attrs
                };


                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("", content);

                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<PatternSmsResponse>(responseText);

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


        public async Task<ResponseSmsModel> SendAsync(SmsJob job)
        {
            string patternCode;
            Dictionary<string, string> attrs = null;

            switch (job.Type)
            {
                // تایید پیش‌فاکتور توسط مشتری
                case SmsType.AcceptPfactorToCustomer:
                    patternCode = smsConfig.Patterns.AcceptPfactorToCustomer;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.SerialNumber,
                        ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                    };
                    break;

                // تایید پیش‌فاکتور توسط تامین
                case SmsType.AcceptPfactorToTamin:
                    patternCode = smsConfig.Patterns.AcceptPfactorToTamin;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.SerialNumber,
                        ["link"] = "Cartable/Index?tab=TTaminExpertId"
                    };
                    break;

                // تایید مدیر فروش (برای مشتری)
                case SmsType.AcceptedSalesAdminToCustomer:
                    patternCode = smsConfig.Patterns.AcceptedSalesAdminToCustomer;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.SerialNumber
                    };
                    break;

                // محصول آماده تحویل (برای مشتری)
                case SmsType.ReadyToDeliverToCustomer:
                    patternCode = smsConfig.Patterns.ReadyToDeliverToCustomer;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.SerialNumber
                    };
                    break;

                // لینک واریز پیش پرداخت (برای مشتری)
                case SmsType.PaymentLinkToCustomer:
                    patternCode = smsConfig.Patterns.PaymentPfactorLink;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.SerialNumber!,
                        ["link"] = job.PfactorId?.ToString().Substring(0, 7)
                    };
                    break;

                // ارسال OTP
                case SmsType.OtpLogin:
                    patternCode = smsConfig.Patterns.OtpLogin;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.Code
                    };
                    break;

                case SmsType.OtpForgot:
                    patternCode = smsConfig.Patterns.OtpForgot;
                    attrs = new Dictionary<string, string> {
                        ["number"] = job.Code
                    };
                    break;

                // chat reminder
                case SmsType.ChatReminder:
                    patternCode = smsConfig.Patterns.ChatReminder;
                    break;

                default:
                    throw new NotSupportedException();
            }

            return await SendPatternSms(job.Mobile, patternCode, attrs);
        }
    }
}