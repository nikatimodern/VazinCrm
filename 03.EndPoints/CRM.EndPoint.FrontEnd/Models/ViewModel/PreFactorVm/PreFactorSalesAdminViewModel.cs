using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.PreFactorVm
{
    public class PreFactorSalesAdminViewModel
    {
        public PreFactor PreFactor { get; set; }
        public decimal ReceiveAmount { get; set; }
        public decimal SumTotalAmountTax { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
