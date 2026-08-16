using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.BaseItems.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Factor;

public class UpdateCustomerVModel {
    public PreFactor PreFactor { get; set; }
    public Customer Customer { get; set; }
    public List<BaseItem> WorkSenf { get; set; }
    public List<BankAccount> BankAccounts { get; set; }
    public ASetting Setting { get; set; }
}