using System.Collections.Generic;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Checks.Entities;

public class Checkbook : BaseEntity {
    //public string AccountNumber { get; set; }
    public string BankName { get; set; }

    public IList<Check> Checks { get; set; }
}