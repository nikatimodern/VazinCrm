using CRM.Core.Domain.AppUsers.Entities;
using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Cartables.Entities
{
    public class BaseAdmin 
    {
        public Guid Id { get; set; }
        public string LatinTitle { get; set; }
        public string PersianTitle { get; set; }
        public bool IsDeleted { get; set; }
       

    }
}
