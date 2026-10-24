using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.Items.Entities;

[Table("ItemStock", Schema = "INV")]
public class ItemStock {
    [Key]
    public int ItemStockID { get; set; }
    public int ItemRef { get; set; }
    public int StockRef { get; set; }
    public int Version { get; set; }

}