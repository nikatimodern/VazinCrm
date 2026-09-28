using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Configuration {
    public class FarazSmsSetting {
        public string User { get; set; }
        public string Pass { get; set; }
        public string LineNumber { get; set; }
        public string Url { get; set; }
        public string ApiKey { get; set; }
        public bool? ActiveStatus { get; set; }
        public SmsPatterns<string> Patterns { get; set; }
    }

    public class SmsPatterns<T> {
        public T AcceptPfactorToCustomer { get; set; }
        public T AcceptedSalesAdminToCustomer { get; set; }
        public T ReadyToDeliverToCustomer { get; set; }
        public T OtpLogin { get; set; }
        public T OtpForgot { get; set; }
        public T AcceptPfactorToTamin { get; set; }
        public T PaymentPfactorLink { get; set; }
        public T ChatReminder { get; set; }
        public T CustomerFeedback { get; set; }
    }

    public class FarazSmsPatternRequest {
        public string code { get; set; }
        public Dictionary<string, string> attributes { get; set; }
        public string recipient { get; set; }
        public string line_number { get; set; }
        public string number_format { get; set; } = "english";
    }

    public class FarazSmsTextRequest {
        public string text { get; set; }
        public string[] recipients { get; set; }
        public string line_number { get; set; }
        public string number_format { get; set; } = "english";
    }

    public class FarazSmsResponse {
        public string status { get; set; }
        public object data { get; set; }
        public string message { get; set; }
    }
}