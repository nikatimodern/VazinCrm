using System.ComponentModel;

namespace CRM.Core.Domain.Accountancies.Enum;

public enum BankAccountTypeEnum {
    [Description("حساب‌های داخلی شرکت")]
    شرکت = 1,
    [Description("تامین کننده")]
    تامین_کننده = 2,
    [Description("تنخواه دار")]
    تنخواه_دار = 3,
    [Description("مشتری")]
    مشتری = 4
}
