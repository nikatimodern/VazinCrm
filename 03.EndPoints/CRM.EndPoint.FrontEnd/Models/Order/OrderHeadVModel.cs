using CRM.Core.Domain.Orders.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Order;

public class OrderHeadVModel {
    public OrderHead OrderHead { get; set; }
    public int RequestAmountCounts { get; set; }
}