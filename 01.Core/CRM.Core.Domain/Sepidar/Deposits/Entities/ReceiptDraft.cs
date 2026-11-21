#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptDraft
{
    public int ReceiptDraftId { get; set; }
    public int Version { get; set; }
    public int ReceiptHeaderRef { get; set; }
    public int HeaderNumber { get; set; }
    public int CurrencyRef { get; set; }
    public int? BankAccountRef { get; set; }
    public DateTime HeaderDate { get; set; }
    public DateTime? Date { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public string? Description_En { get; set; }
    public decimal Rate { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public decimal? Amount { get; set; }
}
