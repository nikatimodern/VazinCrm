using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class GroupedOrdersBySupplierVModal {
    public Guid? SupplierId { get; set; }
    public string SupplierTxt { get; set; }
    public List<ProductGroupVModel> Products { get; set; }
}