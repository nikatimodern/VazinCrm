using System.Collections.Generic;
using CRM.Core.Domain.Cashes.Enums;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.BaseItem;

public class FinancialSettingVModel {
    public List<SelectOptionDto<int>> BaseCashTypes { get; set; }
}