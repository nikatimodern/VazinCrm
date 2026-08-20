using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Orders.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Order {
    public class OrderPrintViewModel {
        public OrderHead OrderHead { get; set; }
        public string Logo { get; set; }
        public Company Company { get; set; }
        public byte OfficialType { get; set; }
        public string SalesExpert { get; set; }
    }
}