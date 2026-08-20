namespace CRM.EndPoint.FrontEnd.Models.Cartable;

public class CartableCountObjVModel {
    public int SalesAdmin { get; set; }
    public SubCartableCountVModel Tamin { get; set; }
    public SubCartableCountVModel Financial { get; set; }
    public SubCartableCountVModel ManagingDirector { get; set; }
}

public class SubCartableCountVModel {
    public int PreFactorCount { get; set; } = 0;
    public int InvoiceCount { get; set; } = 0;
    public int RequestCount { get; set; } = 0;
    public int TransferCount { get; set; } = 0;
}