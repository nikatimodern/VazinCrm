using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Items.Entities;

[Table("Item", Schema = "INV")]
public class Item {
    [Key] public int ItemID { get; set; }

    public int Type { get; set; }

    [MaxLength(250)] public string Code { get; set; } = null!;

    [MaxLength(250)] public string Title { get; set; } = null!;

    [MaxLength(250)] public string Title_En { get; set; } = null!;

    [MaxLength(250)] public string BarCode { get; set; } = null;

    public int UnitRef { get; set; }
    public int? SecondaryUnitRef { get; set; } = null;


    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int? SaleUnitRef { get; private set; }

    public bool IsUnitRatioConstant { get; set; } = true;
    public float? UnitsRatio { get; set; } = null;

    public decimal? MinimumAmount { get; set; } = null;
    public decimal? MaximumAmount { get; set; } = null;

    public bool CanHaveTracing { get; set; } = false;

    public int? TracingCategoryRef { get; set; } = null;
    public bool IsPricingBasedOnTracing { get; set; } = false;

    public bool TaxExempt { get; set; } = false;
    public bool TaxExemptPurchase { get; set; } = false;
    public bool Sellable { get; set; } = true;

    public int? DefaultStockRef { get; set; } = null;
    public int? PurchaseGroupRef { get; set; } = null;
    public int? SaleGroupRef { get; set; } = null;
    public int? CompoundBarcodeRef { get; set; } = null;
    public int? ItemCategoryRef { get; set; } = null;

    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int Version { get; set; }

    public bool IsActive { get; set; }

    public int? AccountSLRef { get; set; } = null;

    public decimal TaxRate { get; set; }
    public decimal DutyRate { get; set; } = 0;

    [MaxLength(250)] public string IranCode { get; set; } = null;

    public int? CodingGroupRef { get; set; } = null;

    public bool SerialTracking { get; set; } = false;

    public decimal? Weight { get; set; } = null;
    public decimal? Volume { get; set; } = null;
    public decimal? ConsumerFee { get; set; } = null;

    public IList<ItemStock> ItemStocks { get; set; }
}