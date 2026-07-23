using System.Collections.Generic;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class BaseCash : BaseEntity {
    public int MoeinCode { get; set; } // کد معین
    public string CostTitle { get; set; } // عنوان هزینه تنخواه
    public string CostItem { get; set; } // مورد هزینه
    public string AccountTitle { get; set; } // عنوان حساب
    public BaseCashTypeEnum BaseCashType { get; set; } // نوع عنوان هزینه
    public string BaseCashTypeTxt { get; set; }

    public ICollection<Cash> Cashes { get; set; }
}