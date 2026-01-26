using CRM.Framework.Domain;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Products.Entities
{
    public class ProductAmount : BaseEntity
    {
        public double From { get; set; }
        public double To { get; set; }
        public string UnitTxt { get; set; }
        public int? Unit { get; set; }
        
        public Guid? ProductId { get; set; }
        public decimal? RasmiAmount { get; set; }
        public decimal? NonRasmiAmount { get; set; }

        [NotMapped]
        public string ElapsTime { get; set; }
    }

}
