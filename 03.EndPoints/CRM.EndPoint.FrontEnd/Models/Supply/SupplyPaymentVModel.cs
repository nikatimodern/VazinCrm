using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Supply;

public class SupplyPaymentVModel {
    public Core.Domain.Payments.Entities.Payment Payment { get; set; }
    public List<string> SupplyFactorSerials { get; set; }
}