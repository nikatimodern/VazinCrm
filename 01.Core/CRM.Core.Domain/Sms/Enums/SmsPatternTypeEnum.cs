using System.ComponentModel;

namespace CRM.Core.Domain.Sms.Enums;

public enum SmsPatternType {
    [Description("تأیید پیش‌فاکتور مشتری")] 
    AcceptPfactorToCustomer,

    [Description("تأیید فروش به مشتری")] 
    AcceptedSalesAdminToCustomer,

    [Description("آماده تحویل به مشتری")] 
    ReadyToDeliverToCustomer,

    [Description("تأیید پیش‌فاکتور تأمین")] 
    AcceptPfactorToTamin,

    [Description("لینک پرداخت پیش‌فاکتور")] 
    PaymentLinkToCustomer,

    [Description("کد ورود")] 
    OtpLogin,

    [Description("کد بازیابی رمز عبور")] 
    OtpForgot,

    [Description("یادآوری چت")] 
    ChatReminder,

    [Description("نظرسنجی مشتری")] 
    CustomerFeedback
}