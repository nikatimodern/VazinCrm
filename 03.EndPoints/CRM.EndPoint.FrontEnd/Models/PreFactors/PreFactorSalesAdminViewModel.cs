using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.PreFactors
{
    public class PreFactorSalesAdminViewModel
    {
        public PreFactorEntity PreFactor { get; set; }
        public decimal ReceiveAmount { get; set; }
        public decimal SumTotalAmountTax { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
