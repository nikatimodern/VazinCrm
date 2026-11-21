#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class DebitCreditNoteItem
{
    public int DebitCreditNoteItemID { get; set; }
    public int DebitCreditNoteRef { get; set; }
    public int RowID { get; set; }
    public int DebitSLRef { get; set; }
    public int CreditSLRef { get; set; }
    public int Version { get; set; }
    public int? DebitDLRef { get; set; }
    public int? CreditDLRef { get; set; }
    public int? DebitType { get; set; }
    public int? CreditType { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public decimal? Rate { get; set; }
    public string? Description { get; set; }
    public string? Description_En { get; set; }
}
