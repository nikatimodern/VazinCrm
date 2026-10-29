using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class Warehouse : BaseEntity {
    public string Title { get; set; }
    public bool IsMain { get; set; }

    public bool CanManage(bool isSeniorManager) => !IsMain || isSeniorManager;
    public WarehouseType WarehouseType { get; set; }
    public ICollection<WarehouseProduct> WarehouseProducts { get; set; }
    public ICollection<WarehouseUser> WarehouseUsers { get; set; }
    public ICollection<WarehouseVoucher> WarehouseVouchers { get; set; }
}

public enum WarehouseType {
    انبار_ضایعات = 0,
    انبار_گردشی = 1,
    انبار_ذخیره = 2,
}