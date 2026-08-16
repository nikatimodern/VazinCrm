namespace CRM.EndPoint.FrontEnd.Models.ViewModel.AccountanciesVm
{
    public class PaymentListViewModel
    {
        public decimal SumTotalAmount { get; set; }
        public decimal TotalTax { get; set; }


        public decimal Discount { get; set; }
        public decimal? SumTotalAmountTax { get; set; }
     
        public decimal ReciveAmount { get; set; }
        public decimal BackwardAmount { get; set; }

        public decimal PaymentAmount { get; set; }
        
    }
}
