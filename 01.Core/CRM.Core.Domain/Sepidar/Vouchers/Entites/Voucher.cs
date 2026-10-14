using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Vouchers.Entites;

[Table("Voucher", Schema = "ACC")]
public class Voucher
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int VoucherId { get; set; }

    public int Number { get; set; }

    public DateTime Date { get; set; }

    public int ReferenceNumber { get; set; }

    public int? SecondaryNumber { get; set; }

    public int State { get; set; }

    public int Type { get; set; }

    public int FiscalYearRef { get; set; }

    [MaxLength(250)]
    public string Description { get; set; }

    [MaxLength(250)]
    public string Description_En { get; set; }

    public int Version { get; set; }

    public int Creator { get; set; }

    public DateTime CreationDate { get; set; }

    public int LastModifier { get; set; }

    public DateTime LastModificationDate { get; set; }

    public int DailyNumber { get; set; }

    public int IssuerSystem { get; set; }

    public bool IsMerged { get; set; }

    public int? MergedIssuerSystem { get; set; }

    public IList<VoucherItem> VoucherItems { get; set; }
}