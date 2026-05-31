using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services.SmsServices {
    public class SmsService {
        private readonly RequestServices request;
        private readonly IConfiguration config;
        private readonly appSettingSms smsConfig;

        public SmsService(RequestServices request, IConfiguration config)
        {
            this.request = request;
            this.config = config;
            this.smsConfig = config.GetSection("smsConfig").Get<appSettingSms>();
        }


        private async Task<ResponseSmsModel> SendPatternSms(string toNumber, string patternCode, string number, string link = null)
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
                Dictionary<string, string> attrs = new() {
                    ["number"] = number
                };

                if (!string.IsNullOrWhiteSpace(link))
                    attrs["link"] = link;

                var model = new PatternSmsRequest {
                    code = patternCode,
                    recipient = toNumber,
                    line_number = smsConfig._fromNumber,
                    attributes = attrs
                };

                using HttpClient client = new();

                client.DefaultRequestHeaders.Add("Api-Key", smsConfig._apiKey);

                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(smsConfig._Url, content);

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

        // تایید پیش‌فاکتور توسط مشتری
        public Task<ResponseSmsModel> SendAcceptPfactorToCustomer(string toNumber, int serialNum, Guid pfactorId)
        {
            var link = pfactorId.ToString().Substring(0, 7);
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptPfactorToCustomer, serialNum.ToString(), link);
        }

        // تایید پیش‌فاکتور توسط تامین
        public Task<ResponseSmsModel> SendAcceptPfactorToTamin(string toNumber, int serialNum)
        {
            var link = "Cartable/Index?tab=TTaminExpertId";
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptPfactorToTamin, serialNum.ToString(), link);
        }

        // تایید مدیر فروش (برای مشتری)
        public Task<ResponseSmsModel> SendAcceptedSalesAdminToCustomer(string toNumber, int serialNum)
        {
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptedSalesAdminToCustomer, serialNum.ToString());
        }

        // محصول آماده تحویل (برای مشتری)
        public Task<ResponseSmsModel> SendReadyToDeliverToCustomer(string toNumber, int? serialNum)
        {
            return SendPatternSms(toNumber, smsConfig.Patterns.ReadyToDeliverToCustomer, serialNum.ToString());
        }

        // لینک واریز پیش پرداخت (برای مشتری)
        public Task<ResponseSmsModel> SendPaymentLinkToCustomer(string toNumber, int serialNum, Guid pfactorId)
        {
            var link = pfactorId.ToString().Substring(0, 7);
            return SendPatternSms(toNumber, smsConfig.Patterns.PaymentPfactorLink, serialNum.ToString(), link);
        }

        // ارسال OTP
        public Task<ResponseSmsModel> SendOtpSms(string mobileNumber, string code, string type)
        {
            string patternCode = type switch {
                "Login" or "Chat" => smsConfig.Patterns.OtpLogin,
                "Forgot" => smsConfig.Patterns.OtpForgot,
                _ => throw new ArgumentException("Invalid OTP type")
            };

            return SendPatternSms(mobileNumber, patternCode, code);
        }


        public async Task<ResponseSmsModel> SendAsync(SmsJob job)
        {
            return job.Type switch
            {
                SmsType.AcceptPfactorToCustomer => await SendAcceptPfactorToCustomer(job.Mobile, job.SerialNumber!.Value, job.PfactorId!.Value)!,

                SmsType.AcceptPfactorToTamin => await SendAcceptPfactorToTamin(job.Mobile, job.SerialNumber!.Value)!,

                SmsType.AcceptedSalesAdminToCustomer => await SendAcceptedSalesAdminToCustomer(job.Mobile, job.SerialNumber!.Value)!,

                SmsType.ReadyToDeliverToCustomer => await SendReadyToDeliverToCustomer(job.Mobile, job.SerialNumber)!,

                SmsType.PaymentLinkToCustomer => await SendPaymentLinkToCustomer(job.Mobile, job.SerialNumber!.Value, job.PfactorId!.Value)!,

                SmsType.OtpLogin => await SendOtpSms(job.Mobile, job.Code, "Login")!,

                SmsType.OtpForgot => await SendOtpSms(job.Mobile, job.Code, "Forgot")!,

                _ => throw new NotSupportedException()
            };
        }
    }
}