using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Cash;

public class CashIndexVModel {
    public bool IsTaminUser { get; set; }
    
    public List<SelectOptionDto<Guid>> CashCenters { get; set; } = [];
    public List<SelectOptionDto<Guid>> CashTitles { get; set; } = [];
}