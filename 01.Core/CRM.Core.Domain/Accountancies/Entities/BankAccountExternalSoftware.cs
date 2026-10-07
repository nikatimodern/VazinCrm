using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Accountancies.Entities;

public class BankAccountExternalSoftware {
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; }
    public string SoftwareName { get; set; }
    public string RefCode { get; set; }
    public string RefId { get; set; }
    
}