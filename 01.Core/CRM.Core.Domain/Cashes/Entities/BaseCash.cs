using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class BaseCash : BaseEntity {
    public int MoeinCode { get; set; } // کد معین
    public string CostTitle { get; set; } // عنوان هزینه تنخواه
    public string CostItem { get; set; } // مورد هزینه
    public string AccountTitle { get; set; } // عنوان حساب
}