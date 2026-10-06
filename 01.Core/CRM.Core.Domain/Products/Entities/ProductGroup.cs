using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Products.Entities
{
    public class ProductGroup
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public IList<Product> Product { get; set; }

    }
}
