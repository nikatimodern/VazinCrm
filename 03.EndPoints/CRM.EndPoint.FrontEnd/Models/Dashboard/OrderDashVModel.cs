using System.Runtime;

namespace CRM.EndPoint.FrontEnd.Models.Dashboard
{
    public class OrderVModelDash
    {
        public int InOrder { get; set; }
        public int InProgress { get; set; }
        public int Done { get; set; }
        public int Deliver { get; set; }
    }
}
