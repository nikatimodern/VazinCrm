using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Customer;

public class SelsCustomerVModel
{
    public Guid customerId { get; set; }
    public List<Guid> selExperts { get; set; }

}