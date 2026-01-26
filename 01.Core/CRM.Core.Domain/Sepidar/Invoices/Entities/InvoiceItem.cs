using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CRM.Core.Domain.Sepidar.Invoices.Entities
{
    public class InvoiceItem
    {
      
        public int InvoiceItemID { get; set; }
        public int InvoiceRef { get; set; }
        public int RowID { get; set; }
        public string ItemCode { get; set; }
        public string ItemTitle { get; set; }
        public decimal Quantity { get; set; }
        public string UnitTitle { get; set; }
        public decimal Fee { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal AdditionFactor { get; set; }
        public decimal PriceInBaseCurrency { get; set; }
        public decimal Tax { get; set; }
        public decimal Duty { get; set; }
        public decimal NetPrice { get; set; }


    }
}
