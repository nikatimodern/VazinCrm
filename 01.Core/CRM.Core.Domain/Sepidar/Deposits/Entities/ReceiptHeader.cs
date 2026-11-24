#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptHeader
{
    public int ReceiptHeaderId { get; set; }
    public int Type { get; set; }
    public int? AccountSlRef { get; set; }
    public int DlRef { get; set; }
    public int Number { get; set; }
    public int CurrencyRef { get; set; }
    public int ItemType { get; set; }
    public int State { get; set; }
    public string CreatorForm { get; set; } = string.Empty;
    public int? Creator { get; set; }
    public int? LastModifier { get; set; }
    public int Version { get; set; }
    public int? CashRef { get; set; }
    public int FiscalYearRef { get; set; }
    public int? VoucherRef { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? LastModificationDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Description_En { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public decimal? Amount { get; set; }
    public decimal? DiscountRate { get; set; }
    public decimal? DiscountInBaseCurrency { get; set; }
    public decimal? TotalAmountInBaseCurrency { get; set; }
}
