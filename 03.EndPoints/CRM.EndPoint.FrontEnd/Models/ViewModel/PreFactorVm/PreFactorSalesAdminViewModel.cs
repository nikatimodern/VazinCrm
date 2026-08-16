using CRM.Core.Domain.PreFactors.Entities;

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
