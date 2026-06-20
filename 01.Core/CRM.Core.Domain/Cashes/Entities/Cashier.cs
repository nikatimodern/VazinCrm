using System;
using CRM.Framework.Domain;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Cashes.Entities {
    public class Cashier : BaseEntity
    {
        public string Title { get; set; }
        public Guid UserId { get; set; }

        public string UserTxt { get; set; }


    }
}