using CRM.Core.Domain.Company.Entities;
using CustomerEntity = CRM.Core.Domain.Customers.Entities.Customer;

namespace CRM.EndPoint.FrontEnd.Models.Factor {
    public class PreviewFactorVModel {
        public CRM.Core.Domain.Factors.Entities.Factor Factor { get; set; }
        public Company Company { get; set; }
        public CustomerEntity _Customer { get; set; }
        public string Siq { get; set; }
        public string Mohr { get; set; }
        public string MohrR { get; set; }
        public string Logo { get; set; }
    }
}