using System;
using CRM.Core.Domain.Checks.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Checks.Entities;

public class Check : BaseEntity {
    public Guid CheckbookId { get; set; }
    public Checkbook Checkbook { get; set; }

    public string SerialNumber { get; set; }

    public decimal Amount { get; set; }
    public string DueDate { get; set; } // تاریخ سررسید چک
    public DateTime? DueDateMiladi { get; set; }
    public string Payee { get; set; } // در وجه (گیرنده چک)
    public Guid? PayeeId { get; set; } // در وجه (گیرنده چک)
    public string Description { get; set; } // توضیحات
    public string SayyadNumber { get; set; } // شماره صیادی

    private CheckStatus _status = CheckStatus.نزد_ما;
    public CheckStatus Status {
        get => _status;
        set {
            _status = value;
            StatusTxt = value.ToString().Replace("_", " ");
        }
    }

    public string StatusTxt { get; private set; }
}