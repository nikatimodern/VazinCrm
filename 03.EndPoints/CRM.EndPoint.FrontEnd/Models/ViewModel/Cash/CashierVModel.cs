using System;
using CRM.Core.Domain.Cashes.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Cash;

public class CashierVModel {
    public Cashier Cashier { get; set; }

    public bool IsAccepted { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal Balance { get; set; }
}