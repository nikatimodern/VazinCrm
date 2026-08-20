using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.BaseItem;

public class FinancialSettingVModel {
    public List<SelectOptionDto<int>> BaseCashTypes { get; set; }
}