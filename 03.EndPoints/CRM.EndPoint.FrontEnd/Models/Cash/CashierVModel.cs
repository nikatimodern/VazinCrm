using System.Collections.Generic;
using CRM.Core.Domain.Cashes.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class CashierVModel {
    public Cashier Cashier { get; set; }
    public List<CashierMappingVModel> CashierMappings { get; set; } = [];
    public bool IsAccepted { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal Balance { get; set; }
}

public class CashierMappingVModel
{
    public string SoftwareTitle { get; set; }
    public string TafsiliCode { get; set; }
}