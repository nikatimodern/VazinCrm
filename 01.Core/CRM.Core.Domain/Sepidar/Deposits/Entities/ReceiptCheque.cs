#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptCheque
{
    public int ReceiptChequeId { get; set; }
    public int Version { get; set; }
    public int State { get; set; }
    public int ReceiptHeaderRef { get; set; }
    public int HeaderNumber { get; set; }
    public int CurrencyRef { get; set; }
    public int DlRef { get; set; }
    public int Type { get; set; }
    public int? CashRef { get; set; }
    public int? BankRef { get; set; }
    public int? InitState { get; set; }
    public DateTime HeaderDate { get; set; }
    public DateTime? Date { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? SecondNumber { get; set; }
    public string? ChequeOwner { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchTitle { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Description_En { get; set; }
    public string? SayadCode { get; set; }
    public bool IsGuarantee { get; set; }
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
}
