using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel;

public class CartableCountObjVModel {
    public int SalesAdmin { get; set; }
    public int Tamin { get; set; }
    public SubCartableCountVModel Financial { get; set; }
    public SubCartableCountVModel ManagingDirector { get; set; }
}

public class SubCartableCountVModel {
    public int PreFactorCount { get; set; }
    public int InvoiceCount { get; set; }
    public int RequestCount { get; set; }
}