using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Sepidar
{
    public class SepidarFactorViewModel
    {
        public Invoice invoice { get; set; }
        public Company Company { get; set; }
       
        public string Siq { get; set; }
        public string Mohr { get; set; }
        public string MohrR { get; set; }
        public string Logo { get; set; }

        public string priceTxt { get; set; }


    }


}
