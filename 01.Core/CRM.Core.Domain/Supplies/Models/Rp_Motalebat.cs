namespace CRM.Core.Domain.Supplies.Models;
public class Rp_Motalebat
{
    public System.Guid? CustomerId { get; set; }
    public string CustomerCode { get; set; }
    public string Customer { get; set; }
    public int PreFactorCount { get; set; }
    public decimal PreFactorSum { get; set; }
    public int FactorCount { get; set; }
    public decimal FactorSum { get; set; }
    public int AccCount { get; set; }
    public decimal AccSum { get; set; }
    public int RetAccBackCount { get; set; }
    public decimal RetAccBackSum { get; set; }
    public decimal DebitSum { get; set; }
    public decimal ReceivableSum => System.Math.Max(DebitSum, 0m);
    public decimal DebtSum => System.Math.Min(DebitSum, 0m);
}
