using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Voucher.Entites;

[Table("VoucherItem", Schema = "ACC")]
public class VoucherItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int VoucherItemId { get; set; }

    public int VoucherRef { get; set; }

    public int RowNumber { get; set; }

    public int AccountSLRef { get; set; }

    public int? DLRef { get; set; }

    [Column(TypeName = "decimal(28,4)")]
    public decimal? Debit { get; set; }

    [Column(TypeName = "decimal(28,4)")]
    public decimal? Credit { get; set; }

    public int? CurrencyRef { get; set; }

    [Column(TypeName = "decimal(26,16)")]
    public decimal? CurrencyRate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? CurrencyDebit { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? CurrencyCredit { get; set; }

    [MaxLength(40)]
    public string TrackingNumber { get; set; }

    public DateTime? TrackingDate { get; set; }

    [Required]
    [MaxLength(400)]
    public string IssuerEntityName { get; set; } = string.Empty;

    public int IssuerEntityRef { get; set; }

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description_En { get; set; } = string.Empty;

    public int Version { get; set; }
}