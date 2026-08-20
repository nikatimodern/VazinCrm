using System;

namespace CRM.EndPoint.FrontEnd.Models.Customer;

public class CustomerPanelVModel {
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string SelExpertRel { get; set; }
}