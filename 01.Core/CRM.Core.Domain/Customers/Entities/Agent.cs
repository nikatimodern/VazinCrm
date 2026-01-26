using CRM.Core.Domain.Contacts.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Customers.Entities
{
    public class Agent : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MeliCode { get; set; }
        public Guid? CustomerId { get; set; }
        public int? CustomerCode { get; set; }
        public IList<Contact> Contacts { get; set; }

    }

}


