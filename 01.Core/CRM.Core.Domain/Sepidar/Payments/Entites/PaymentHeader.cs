using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Payments.Entites;

[Table("PaymentHeader", Schema = "RPA")]
public class PaymentHeader
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int PaymentHeaderId { get; set; }

    public int Type { get; set; }

    public int? AccountSlRef { get; set; }

    public int DlRef { get; set; }

    public int Number { get; set; }

    public DateTime Date { get; set; }

    public int CurrencyRef { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description_En { get; set; } = string.Empty;

    [Column(TypeName = "decimal(19,4)")]
    public decimal? Discount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? TotalAmount { get; set; }

    public int ItemType { get; set; }

    public int State { get; set; }

    [Required]
    [MaxLength(50)]
    public string CreatorForm { get; set; } = string.Empty;

    public int? Creator { get; set; }

    public DateTime? CreationDate { get; set; }

    public int? LastModifier { get; set; }

    public DateTime? LastModificationDate { get; set; }

    public int Version { get; set; }

    [Column(TypeName = "decimal(26,16)")]
    public decimal Rate { get; set; }

    public int? CashRef { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? Amount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    public int? ReceiptHeaderRef { get; set; }

    public int FiscalYearRef { get; set; }

    public int? VoucherRef { get; set; }
    public Vouchers.Entites.Voucher Voucher { get; set; }

    [Column(TypeName = "decimal(26,16)")]
    public decimal? DiscountRate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? DiscountInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? TotalAmountInBaseCurrency { get; set; }

    public int? BankFeeSlRef { get; set; }

    // Computed column in SQL Server
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "decimal(20,4)")]
    public decimal? PaymentAmount { get; private set; }


    public IList<PaymentDraft> PaymentDrafts { get; set; }
}