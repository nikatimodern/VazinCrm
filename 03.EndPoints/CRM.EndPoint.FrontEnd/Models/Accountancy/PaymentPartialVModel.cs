using System.Collections.Generic;
using AccountancyEntity = CRM.Core.Domain.Accountancies.Entities.Accountancy;

namespace CRM.EndPoint.FrontEnd.Models.Accountancy;

public class PaymentPartialVModel {
    public bool ReadOnly { get; set; }
    public IList<AccountancyEntity> Items { get; set; }

    public string UserId { get; set; }
    public bool IsCreatorParent { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSalesAdmin { get; set; }
    public bool IsManagingDirector { get; set; }
    public bool IsTamin { get; set; }
    public bool IsFinancial { get; set; }
    public bool IsCustomer { get; set; }

    public int Type { get; set; }

    public int PassType { get; set; }
    /*
    PassType = 1: پروفایل مشتری
    PassType = 2: کارتابل پیش‌فاکتور و فاکتور
    PassType = 3: کارتابل کارشناس فروش
    PassType = 4: سفارشات
    PassType = 5: (پیش فاکتور) کارتابل مالی و مدیرعامل
    PassType = 55: کارتابل مالی (صورتحساب)
    PassType = 56: کارتابل مدیرعامل (صورتحساب)
    PassType = 6: ایمیل
    */
}
