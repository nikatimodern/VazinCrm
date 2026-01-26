namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Reports;

public class Rp_Motalebat {


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

}