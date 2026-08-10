using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.EndPoint.FrontEnd.Models.PreFactorVm;

public class PreFactorFinancialExpertVModel {

    public PreFactor PreFactor { get; set; }
    public int NotAcceptedFiAccCount { get; set; }
    public int NotAcceptedMdAccCount { get; set; }
    public int TotalAccCount { get; set; }
    public decimal ReceiveAmount { get; set; }

}