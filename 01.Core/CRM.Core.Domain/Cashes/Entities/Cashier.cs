using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Cashes.Entities
{
    public class Cashier:BaseEntity
    {

        public string UserId { get; set; }
        public IList<Cash> Cashes { get; set; }


    }
}
