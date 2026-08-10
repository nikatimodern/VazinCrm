using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel
{
    public class DuplicateViewModel
    {
        public List<CRM.Core.Domain.Customers.Entities.Customer> Customers { get; set; }
        public string ViewPartial { get; set; }



    }
}
