using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CRM.Core.Domain.Products.Entities
{
    public class Product:BaseEntity
    {
        public string Title { get; set; }
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public bool IsInProduction { get; set; } = true;
        public IList<ProductAmount> ProductAmounts { get; set; }


        //public decimal? RasmiAmount { get; set; }
        //public decimal? NonRasmiAmount { get; set; }
    }

}
