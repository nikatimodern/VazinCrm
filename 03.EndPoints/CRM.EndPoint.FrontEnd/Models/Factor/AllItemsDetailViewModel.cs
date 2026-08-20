using System;

namespace CRM.EndPoint.FrontEnd.Models.Factor
{
    public class AllItemsDetailViewModel
    {
        public Guid? facId { get; set; }
        public string Product { get; set; }
        public int FserialNumber { get; set; }
        public string Date { get; set; }
        public string FWeightCount { get; set; }
        public decimal Amount { get; set; }


    }
}
