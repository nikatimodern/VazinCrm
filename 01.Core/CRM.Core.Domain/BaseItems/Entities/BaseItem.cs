using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.BaseItems.Entities
{
    public class BaseItem
    {
        public Guid Id { get; set; }
        public int TypeCode { get; set; }
        public string TypeTxt { get; set; }
        public string Title { get; set; }
    }
}
