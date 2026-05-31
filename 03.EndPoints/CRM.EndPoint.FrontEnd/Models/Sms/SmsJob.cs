using System;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

public class SmsJob {
    public SmsType Type { get; set; }
    public string Mobile { get; set; }
    public int? SerialNumber { get; set; }
    public Guid? PfactorId { get; set; }
    public string Code { get; set; }
    public string UserId { get; set; }
}

public enum SmsType {
    AcceptPfactorToCustomer,
    AcceptPfactorToTamin,
    AcceptedSalesAdminToCustomer,
    ReadyToDeliverToCustomer,
    PaymentLinkToCustomer,
    OtpLogin,
    OtpForgot
}