using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Customers.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Factor
{
    public class FactorViewModel
    {
        public CRM.Core.Domain.Factors.Entities.Factor Factor { get; set; }
        public Company Company { get; set; }
        public Customer _Customer { get; set; }
        public string Siq { get; set; }
        public string Mohr { get; set; }
        public string MohrR { get; set; }
        public string Logo { get; set; }




    }


}
