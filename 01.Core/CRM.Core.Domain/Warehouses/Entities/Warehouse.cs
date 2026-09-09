using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class Warehouse : BaseEntity {
    public string Title { get; set; }
    public ICollection<WarehouseProduct> WarehouseProducts { get; set; }
    public ICollection<WarehouseUser> WarehouseUsers { get; set; }
    public ICollection<WarehouseVoucher> WarehouseVouchers { get; set; }
}