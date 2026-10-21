using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Stocks;


[Table("Stock", Schema = "INV")]
public class Stock {
    
    [Key] 
    public int StockID { get; set; }
    public int Code { get; set; }
    
    [MaxLength(250)]
    public string Title { get; set; }
    public bool IsActive { get; set; }
    public int AccountSLRef { get; set; }
}