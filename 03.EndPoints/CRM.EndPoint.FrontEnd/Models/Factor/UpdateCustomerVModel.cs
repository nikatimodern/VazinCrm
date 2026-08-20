using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ASettings.Entities;
using BaseItemEntity = CRM.Core.Domain.BaseItems.Entities.BaseItem;
using CustomerEntity = CRM.Core.Domain.Customers.Entities.Customer;
using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.Factor;

public class UpdateCustomerVModel {
    public PreFactorEntity PreFactor { get; set; }
    public CustomerEntity Customer { get; set; }
    public List<BaseItemEntity> WorkSenf { get; set; }
    public List<BankAccount> BankAccounts { get; set; }
    public ASetting Setting { get; set; }
}