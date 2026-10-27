using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Supplies.Models;
public class GroupedOrdersBySupplierVModal
{
    public Guid? SupplierId { get; set; }
    public string SupplierTxt { get; set; }
    public List<ProductGroupVModel> Products { get; set; }
}
