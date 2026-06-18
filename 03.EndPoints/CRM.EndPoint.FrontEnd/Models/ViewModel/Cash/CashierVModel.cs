using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Cash;

public class CashierVModel {
    public Guid Id { get; set; }
    public string CashierId { get; set; }
    public string CashierTxt { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public bool isAccepted { get; set; }
}