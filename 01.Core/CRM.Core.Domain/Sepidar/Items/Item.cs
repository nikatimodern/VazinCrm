using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar;

[Table("Item", Schema = "INV")]
public class Item {
    [Key]
    public int ItemID { get; set; }

    public int Type { get; set; }

    [MaxLength(250)]
    public string Code { get; set; } = null!;

    [MaxLength(250)]
    public string Title { get; set; } = null!;

    [MaxLength(250)]
    public string Title_En { get; set; } = null!;

    [MaxLength(250)]
    public string BarCode { get; set; }

    public int UnitRef { get; set; }
    public int? SecondaryUnitRef { get; set; }

    public int? SaleUnitRef { get; private set; }

    public bool IsUnitRatioConstant { get; set; }
    public float? UnitsRatio { get; set; }

    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }

    public bool CanHaveTracing { get; set; }

    public int? TracingCategoryRef { get; set; }
    public bool IsPricingBasedOnTracing { get; set; }

    public bool TaxExempt { get; set; }
    public bool TaxExemptPurchase { get; set; }
    public bool Sellable { get; set; }

    public int? DefaultStockRef { get; set; }
    public int? PurchaseGroupRef { get; set; }
    public int? SaleGroupRef { get; set; }
    public int? CompoundBarcodeRef { get; set; }
    public int? ItemCategoryRef { get; set; }

    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int Version { get; set; }

    public bool IsActive { get; set; }

    public int? AccountSLRef { get; set; }

    public decimal TaxRate { get; set; }
    public decimal DutyRate { get; set; }

    [MaxLength(250)]
    public string IranCode { get; set; }

    public int? CodingGroupRef { get; set; }

    public bool SerialTracking { get; set; }

    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public decimal ConsumerFee { get; set; }
}