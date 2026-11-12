using System.ComponentModel;

namespace CRM.Core.Domain.RequestAmounts.Enums;

public enum RequestAmountTypeEnum {
    [Description("پیش پرداخت")]
    پیش_پرداخت = 1,
    [Description("پرداخت فاکتور")]
    پرداخت_فاکتور = 2,
    [Description("تنخواه")]
    تنخواه = 3,
    [Description("بابت بدهی")]
    بابت_بدهی = 4,
    [Description("بابت تضمین")]
    بابت_تضمین = 5,
    [Description("عودت به مشتری")]
    عودت_به_مشتری = 6
}