using System;
using CRM.Core.Domain.Cashes.Enums;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class CashGroupVModel {
    public int ItemsCount { get; set; }
    public Guid? CashGroupId { get; set; }
    public int GroupNumber { get; set; }
    public string UserCreator { get; set; }
    public string CreateDateTxt { get; set; }
    public decimal TotalAmount { get; set; }
    public CashStatusEnum Status { get; set; }
    public string StatusTxt { get; set; }
    public DateTime CreateDate { get; set; }
}