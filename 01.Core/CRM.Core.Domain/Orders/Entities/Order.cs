using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Orders.Entities
{
    public class Order:BaseEntity
    {
        public string Product { get; set; }
        public string Count { get; set; }//تعداد
        //public string Weight { get; set; }
        public string Value { get; set; }//مقدار
        public string Level { get; set; }
        public string ProductSmallName { get; set; }
        public Guid? ProductGroup { get; set; }
        public string ProductGroupTxt { get; set; }

        public Guid? ProductCode { get; set; }
        public string Dimensions { get; set; }
        public string CategoryType { get; set; }
        public string Caset { get; set; }
        public string BackgroundColor { get; set; }
        public string PrintColor { get; set; }
        public string CountColor { get; set; }
        public string Weight { get; set; }
        public int? Unit { get; set; } = 0;
        public string? UnitTxt { get; set; } = string.Empty;
        public string Thickness { get; set; }
        public string DesignName { get; set; }

        public bool? Delivered { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public OrderHead OrderHead { get; set; }
        public Guid? OrderHeadId { get; set; }

        public int Row { get; set; }

        public int Status { get; set; } = 0;
        public string StatusTxt { get; set; } =string.Empty;

        public Guid? PreFactorItemId { get; set; }

    }



}
