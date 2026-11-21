#nullable enable

using System;

namespace CRM.Core.Domain.Sepidar.Deposits.Entities;

public sealed class ReceiptChequeBanking
{
    public int ReceiptChequeBankingId { get; set; }
    public DateTime Date { get; set; }
    public int Type { get; set; }
    public int? RelationNo { get; set; }
    public int? BankAccountRef { get; set; }
    public int Number { get; set; }
    public int? Creator { get; set; }
    public int Version { get; set; }
    public int? ReceiptChequeBankingRef { get; set; }
    public int? CashRef { get; set; }
    public int State { get; set; }
    public DateTime? CreationDate { get; set; }
    public int? LastModifier { get; set; }
    public DateTime? LastModificationDate { get; set; }
    public int FiscalYearRef { get; set; }
    public int? VoucherRef { get; set; }
    public string? SubmitNumber { get; set; }
}
