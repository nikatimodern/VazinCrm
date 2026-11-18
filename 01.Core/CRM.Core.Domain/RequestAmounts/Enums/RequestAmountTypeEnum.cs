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

// Each request type maps its target to either Customers or Cashiers.
public enum RequestAmountTargetKind
{
    Supplier,
    Cashier,
    Customer
}

public static class RequestAmountTypeExtensions
{
    public static RequestAmountTargetKind GetTargetKind(this RequestAmountTypeEnum type) => type switch
    {
        RequestAmountTypeEnum.تنخواه => RequestAmountTargetKind.Cashier,
        RequestAmountTypeEnum.عودت_به_مشتری => RequestAmountTargetKind.Customer,
        _ => RequestAmountTargetKind.Supplier
    };
}
