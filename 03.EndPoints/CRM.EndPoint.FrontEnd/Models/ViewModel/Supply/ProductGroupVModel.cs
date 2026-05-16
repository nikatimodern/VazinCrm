using System;
using System.Collections.Generic;
using CRM.Core.Domain.Orders.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class ProductGroupVModel {
    public Guid ProductCode { get; set; }
    public List<Core.Domain.Orders.Entities.Order> Orders { get; set; }
    public OrderHead OrderHead { get; set; }
    public decimal SumWeight { get; set; }
    public string ProductSmallName { get; set; }
    public string UnitTxt { get; set; }
}