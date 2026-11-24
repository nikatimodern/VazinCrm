#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptChequeBankingItem
{
    public int ReceiptChequeBankingItemId { get; set; }
    public int ReceiptChequeBankingRef { get; set; }
    public int ReceiptChequeRef { get; set; }
    public int? ReceiptChequeBankingItemRef { get; set; }
    public DateTime? ForcastDate { get; set; }
    public int State { get; set; }
    public DateTime HeaderDate { get; set; }
    public int HeaderNumber { get; set; }
    public int? BankAccountRef { get; set; }
    public int? CashRef { get; set; }
    public int HeaderState { get; set; }
}
