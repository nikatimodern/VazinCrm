using System;
using CRM.Core.Domain.BaseItems.Enums;

namespace CRM.Core.Domain.BaseItems.Entities
{
    public class BaseItem
    {
        public Guid Id { get; set; }
        public BaseItemEnum TypeCode { get; set; }
        public string TypeTxt { get; set; }
        public string Title { get; set; }
    }
}
