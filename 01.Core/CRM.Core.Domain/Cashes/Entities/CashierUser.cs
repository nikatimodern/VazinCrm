using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class CashierUser : JunctionBaseEntity
{
    public Guid CashierId { get; set; }
    public string UserId { get; set; }
}