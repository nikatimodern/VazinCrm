using System;
using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class WarehouseVoucher : BaseEntity { // حواله انبار
    public int VoucherNumber { get; set; }
    public int Status { get; set; }
    public IList<WarehouseProductVoucher> WarehouseProductVouchers { get; set; }
}