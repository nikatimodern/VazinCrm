using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Payments.Entites;

[Table("PaymentDraft", Schema = "RPA")]
public class PaymentDraft {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int PaymentDraftId { get; set; }

    [MaxLength(60)] public string Number { get; set; }

    public DateTime? Date { get; set; }

    [Column(TypeName = "decimal(19,4)")] public decimal Amount { get; set; }

    public int? BankAccountRef { get; set; }

    [MaxLength(4000)] public string Description { get; set; }

    [MaxLength(4000)] public string Description_En { get; set; }

    public int Version { get; set; }

    public int PaymentHeaderRef { get; set; }

    public int HeaderNumber { get; set; }

    public DateTime HeaderDate { get; set; }

    public int CurrencyRef { get; set; }

    [Column(TypeName = "decimal(26,16)")] public decimal Rate { get; set; }

    [Column(TypeName = "decimal(19,4)")] public decimal AmountInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(19,4)")] public decimal? BankFeeAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")] public decimal? BankFeeAmountInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(26,16)")] public decimal? BankFeeAmountRate { get; set; }
}