using System;
using System.Collections.Generic;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseVoucher : BaseEntity { // حواله انبار
    public int VoucherNumber { get; set; }

    public WarehouseVoucherStatusEnum Status {
        get;
        set {
            field = value;
            StatusTxt = value.ToString().Replace("_", " ");
        }
    }

    public string StatusTxt { get; private set; }
    public IList<WarehouseProductVoucher> WarehouseProductVouchers { get; set; }
}