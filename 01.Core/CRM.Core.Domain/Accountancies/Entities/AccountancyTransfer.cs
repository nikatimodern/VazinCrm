using System;
using System.ComponentModel.DataAnnotations;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Accountancies.Entities;

public class AccountancyTransfer : JunctionBaseEntity {
    [Key]
    public Guid AccountancyId { get; set; }
    public string Beneficiary { get; set; } // بابت بدهی به
    public string ToName { get; set; } //  واریز به نام
}