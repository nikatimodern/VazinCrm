using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.SaleTypes;

[Table("SaleType", Schema = "SLS")]
public class SaleType {
    
    [Key] 
    public int SaleTypeId { get; set; }
    public int Number { get; set; }
    
    [MaxLength(50)]
    public string Title { get; set; }
}