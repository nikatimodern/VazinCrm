using System;
using System.Collections.Generic;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Checks.Entities;

public class Checkbook : BaseEntity {
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; }
    public IList<Check> Checks { get; set; }
}