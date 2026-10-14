using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Cash;

public class CashIndexVModel {
    public bool IsTaminUser { get; set; }
    
    public List<SelectOptionDto<Guid>> CashCenters { get; set; } = [];
    public List<SelectOptionDto<Guid>> CashTitles { get; set; } = [];
    public List<SelectOptionDto<Guid>> CashierTitles { get; set; } = [];
}