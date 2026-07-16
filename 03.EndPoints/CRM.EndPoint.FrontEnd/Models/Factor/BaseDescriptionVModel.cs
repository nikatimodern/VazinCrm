using CRM.Core.Domain.PreFactors.Enum;

namespace CRM.EndPoint.FrontEnd.Models.Factor;

public class BaseDescriptionVModel {
    public string Text { get; set; }
    public bool? Official { get; set; }
    public BaseDescriptionTypeEnum Type { get; set; }
}