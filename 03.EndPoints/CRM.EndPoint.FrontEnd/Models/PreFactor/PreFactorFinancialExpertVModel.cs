using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.PreFactor;

public class PreFactorFinancialExpertVModel {
    public PreFactorEntity PreFactor { get; set; }
    public int NotAcceptedFiAccCount { get; set; }
    public int NotAcceptedMdAccCount { get; set; }
    public int TotalAccCount { get; set; }
    public decimal ReceiveAmount { get; set; }
}