using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models {
    public class appSettingSms {
        public string _user { get; set; }
        public string _pass { get; set; }
        public string _fromNumber { get; set; }
        public string _Url { get; set; }
        public string _apiKey { get; set; }
        public bool? ActiveStatus { get; set; }
        public SmsPatterns Patterns { get; set; }
    }

    public class SmsPatterns {
        public string AcceptPfactorToCustomer { get; set; }
        public string AcceptedSalesAdminToCustomer { get; set; }
        public string ReadyToDeliverToCustomer { get; set; }
        public string OtpLogin { get; set; }
        public string OtpForgot { get; set; }
        public string AcceptPfactorToTamin { get; set; }
        public string PaymentPfactorLink { get; set; }
    }

    public class PatternSmsRequest {
        public string code { get; set; }
        public Dictionary<string, string> attributes { get; set; }
        public string recipient { get; set; }
        public string line_number { get; set; }
        public string number_format { get; set; } = "english";
    }

    public class PatternSmsResponse {
        public string status { get; set; }
        public object data { get; set; }
        public string message { get; set; }
    }
}