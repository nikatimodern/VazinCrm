using System.Collections.Generic;

namespace CRM.Core.ApplicationServices.Accountancies.Dtos;
public sealed record AccountancyOutcome(int Status, string? Message = null);
public class PaymentPartialData
{
    public IList<CRM.Core.Domain.Accountancies.Entities.Accountancy> Items { get; set; } = [];
    public string UserId { get; set; } = string.Empty;
    public bool IsCreatorParent { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSalesAdmin { get; set; }
    public bool IsManagingDirector { get; set; }
    public bool IsTamin { get; set; }
    public bool IsFinancial { get; set; }
    public bool IsCustomer { get; set; }
    public int Type { get; set; }
    public int PassType { get; set; }
}

public class PaymentTotalsData
{
    public decimal SumTotalAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal Discount { get; set; }
    public decimal? SumTotalAmountTax { get; set; }
    public decimal ReciveAmount { get; set; }
    public decimal BackwardAmount { get; set; }
    public decimal PaymentAmount { get; set; }
}
