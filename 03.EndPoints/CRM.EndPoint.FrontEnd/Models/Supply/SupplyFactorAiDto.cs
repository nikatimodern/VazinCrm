using System;
using System.Collections.Generic;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework;

namespace CRM.EndPoint.FrontEnd.Models.Supply;

public class SupplyFactorAiDto {
    public byte OfficialType { get; set; }
    public string SerialNumber { get; set; }

    public string Date {
        get;
        init {
            field = value;
            DateMiladi = BaseServices.GetDateMiladi(value);
        }
    }

    public DateTime? DateMiladi { get; private set; }

    public List<SupplyFactorAiItemDto> Items { get; set; } = [];
}

public class SupplyFactorAiItemDto {
    public string ProductSmallName { get; set; }
    public Guid? ProductCode { get; set; }
    public string Product { get; set; }
    public byte Printable { get; set; }
    public decimal WeightCount { get; set; }

    public UnitTypeEnums? Unit {
        get;
        set {
            field = value;
            UnitTxt = value?.ToString().Replace("_", " ");
        }
    }

    public string UnitTxt { get; private set; }

    public decimal UnitAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}