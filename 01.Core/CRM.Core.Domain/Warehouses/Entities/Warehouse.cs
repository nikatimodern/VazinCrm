using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Warehouses.Entities;

public class Warehouse : BaseEntity {
    public string Title { get; set; }
    public ICollection<WarehouseProduct> WarehouseProducts { get; set; }
}