using System;

namespace CRM.EndPoint.FrontEnd.Models.Supply;

public class SupplierFinancialSummaryVModel {
    public Guid SupplierId { get; set; }
    public decimal TotalSupplyAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal SupplierDebt { get; set; }
}