#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class DebitCreditNote
{
    public int DebitCreditNoteID { get; set; }
    public int Number { get; set; }
    public int CurrencyRef { get; set; }
    public int Version { get; set; }
    public int Creator { get; set; }
    public int LastModifier { get; set; }
    public int FiscalYearRef { get; set; }
    public int? VoucherRef { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastModificationDate { get; set; }
    public decimal Rate { get; set; }
    public decimal SumAmount { get; set; }
    public decimal? SumAmountInBaseCurrency { get; set; }
    public string? Description { get; set; }
    public string? Description_En { get; set; }
}
