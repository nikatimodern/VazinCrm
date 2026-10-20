using System;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class CashierBalanceDto {
    public Guid CashierId { get; set; }
    public decimal? TotalCredit { get; set; }
    public decimal? TotalDebit { get; set; }
    public bool isAccepted { get; set; }
}