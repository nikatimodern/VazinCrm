using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services {
    public class SmsServices {
        private readonly RequestServices request;
        private readonly IConfiguration config;
        private readonly appSettingSms smsConfig;

        public SmsServices(RequestServices request, IConfiguration config) {
            this.request = request;
            this.config = config;
            this.smsConfig = config.GetSection("smsConfig").Get<appSettingSms>();
        }

        private async Task<ResponseSmsModel> SendPatternSms(string toNumber, string patternCode, string number = null, string link = null) {
            var res = new ResponseSmsModel();

            if (smsConfig.ActiveStatus == false) {
                res.code = 0;
                res.Desc = "ارسال پیامک غیر فعال است";
                return res;
            }

            var inp = new List<InDataPaternAccepting> {
                new InDataPaternAccepting {
                    number = number,
                    link = link
                }
            };

            var model = new SendMessagePaternModel {
                op = "pattern",
                user = smsConfig._user,
                pass = smsConfig._pass,
                fromNum = smsConfig._fromNumber,
                toNum = toNumber,
                patternCode = patternCode,
                inputData = inp
            };

            var response = await request.CreateRequestAsync(smsConfig._Url, JsonConvert.SerializeObject(model), "POST");

            if (response.Contains('[') && response.Contains(']')) {
                JsonNode js = JsonNode.Parse(response);
                if (js is JsonArray) {
                    res.code = int.Parse(js[0]?.ToString());
                    res.Desc = js[1]?.ToString();
                }
            } else {
                res.code = 200;
                res.Desc = response;
            }

            return res;
        }

        // تایید پیش‌فاکتور توسط مشتری
        public Task<ResponseSmsModel> SendAcceptPfactorToCustomer(string toNumber, int serialNum, Guid pfactorId) {
            var link = pfactorId.ToString().Substring(0, 7);
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptPfactorToCustomer, serialNum.ToString(), link);
        }

        // تایید پیش‌فاکتور توسط تامین
        public Task<ResponseSmsModel> SendAcceptPfactorToTamin(string toNumber, int serialNum) {
            var link = "Cartable/Index?tab=TTaminExpertId";
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptPfactorToTamin, serialNum.ToString(), link);
        }

        // تایید مدیر فروش (برای مشتری)
        public Task<ResponseSmsModel> SendAcceptedSalesAdminToCustomer(string toNumber, int serialNum) {
            return SendPatternSms(toNumber, smsConfig.Patterns.AcceptedSalesAdminToCustomer, serialNum.ToString());
        }

        // محصول آماده تحویل (برای مشتری)
        public Task<ResponseSmsModel> SendReadyToDeliverToCustomer(string toNumber, int? serialNum) {
            return SendPatternSms(toNumber, smsConfig.Patterns.ReadyToDeliverToCustomer, serialNum.ToString());
        }

        // لینک واریز پیش پرداخت (برای مشتری)
        public Task<ResponseSmsModel> SendPaymentLinkToCustomer(string toNumber, int serialNum, Guid pfactorId) {
            var link = pfactorId.ToString().Substring(0, 7);
            return SendPatternSms(toNumber, smsConfig.Patterns.PaymentPfactorLink, serialNum.ToString(), link);
        }

        // ارسال OTP
        public Task<ResponseSmsModel> SendOtpSms(string mobileNumber, string code, string type) {
            string patternCode = type switch {
                "Login" or "Chat" => smsConfig.Patterns.OtpLogin,
                "Forgot" => smsConfig.Patterns.OtpForgot,
                _ => throw new ArgumentException("Invalid OTP type")
            };

            return SendPatternSms(mobileNumber, patternCode, code);
        }


        // chat reminder
        public Task<ResponseSmsModel> SendChatRemind(string toNumber) {
            return SendPatternSms(toNumber, smsConfig.Patterns.ChatReminder);
        }
    }
}