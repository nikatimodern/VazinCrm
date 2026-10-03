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
}