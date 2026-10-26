using System.Collections.Generic;
using System;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Supplies.Entities;

public class SupplyFactorItem : BaseEntity {
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<SupplyFactorItemAllocation> Allocations { get; set; } = new List<SupplyFactorItemAllocation>();

    public string Product { get; set; }

    public Guid? ProductCode { get; set; }

    public string ProductSmallName { get; set; }

    public decimal WeightCount { get; set; }

    private UnitTypeEnums _unit;

    public UnitTypeEnums Unit {
        get => _unit;
        set {
            _unit = value;
            UnitTxt = value.ToString().Replace("_", " ");
        }
    }

    public string UnitTxt { get; private set; }

    public decimal UnitAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxAmount { get; set; }

    public byte? Printable { get; set; } = 0;

    public Guid SupplyFactorId { get; set; }
    public SupplyFactor SupplyFactor { get; set; }
}