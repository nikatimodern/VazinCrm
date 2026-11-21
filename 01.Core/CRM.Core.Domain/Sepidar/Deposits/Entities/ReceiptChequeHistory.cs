#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptChequeHistory
{
    public int ReceiptChequeHistoryId { get; set; }
    public int State { get; set; }
    public int Type { get; set; }
    public int ReceiptChequeRef { get; set; }
    public DateTime Date { get; set; }
}
