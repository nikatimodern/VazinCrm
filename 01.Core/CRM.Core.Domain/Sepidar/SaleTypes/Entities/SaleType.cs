using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.SaleTypes.Entities;

[Table("SaleType", Schema = "SLS")]
public class SaleType {
    
    [Key] 
    public int SaleTypeId { get; set; }
    public int Number { get; set; }
    
    [MaxLength(50)]
    public string Title { get; set; }

    // Accounting accounts defined separately for each Sepidar sale type.
    // These are ACC.Account.AccountId values, not textual Moein codes.
    public int? PartSalesSLRef { get; set; }
    public int? ServiceSalesSLRef { get; set; }
    public int? PartSalesReturnSLRef { get; set; }
    public int? ServiceSalesReturnSLRef { get; set; }
    public int? PartSalesDiscountSLRef { get; set; }
    public int? ServiceSalesDiscountSLRef { get; set; }
    public int? SalesAdditionSLRef { get; set; }
}