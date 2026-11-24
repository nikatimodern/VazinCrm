#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class PaymentChequeOther
{
    public int PaymentChequeOtherId { get; set; }
    public int ReceiptChequeRef { get; set; }
    public int PaymentHeaderRef { get; set; }
    public int HeaderNumber { get; set; }
    public DateTime HeaderDate { get; set; }
    public string? Description { get; set; }
    public string? Description_En { get; set; }
}
