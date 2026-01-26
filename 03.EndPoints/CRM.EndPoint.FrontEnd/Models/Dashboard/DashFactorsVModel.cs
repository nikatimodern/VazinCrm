namespace CRM.EndPoint.FrontEnd.Models.Dashboard
{
    public class DashFactorsVModel
    {
        public int Count { get; set; } = 0;
        public int Export { get; set; } = 0;
        public int NoExport { get; set; } = 0;
        public decimal SumAmount { get; set; }
    }
}
