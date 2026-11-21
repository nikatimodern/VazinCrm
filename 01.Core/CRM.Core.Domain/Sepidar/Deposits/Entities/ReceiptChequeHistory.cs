#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptChequeHistory
{
    public int ReceiptChequeHistoryId { get; set; }
    public int State { get; set; }
    public int Type { get; set; }
    public int ReceiptChequeRef { get; set; }
    public int? ReceiptChequeHistoryRef { get; set; }
    public int? ReceiptChequeBankingItemRef { get; set; }
    public int? PaymentChequeOtherRef { get; set; }
    public DateTime Date { get; set; }
}
