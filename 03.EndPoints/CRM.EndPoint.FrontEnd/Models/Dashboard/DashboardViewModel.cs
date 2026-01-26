namespace CRM.EndPoint.FrontEnd.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int? AllCustomers { get; set; }
        public int? CustomerJur { get; set; }
        public int? CustomerNotJur { get; set; }
        public int? AllPrefactor { get; set; }
        public decimal AllPrefactorAmount { get; set; }

        public int? AllPrefactorAccepted { get; set; }
        public decimal AllPrefactorAcceptedAmount { get; set; }

    }
}
