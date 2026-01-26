using CRM.Core.Domain.Contacts.Enum;
using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Contacts.Entities
{
    public class Contact : BaseEntity
    {
        public string Number { get; set; }
        public ContactTypeEnum ContactType { get; set; }
        public string ContactTypeTxt { get; set; }
        public Guid? EntityId { get; set; }

    }

}


