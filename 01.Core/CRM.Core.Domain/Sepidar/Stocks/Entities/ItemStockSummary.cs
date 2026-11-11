using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Sepidar.Items.Entities;

namespace CRM.Core.Domain.Sepidar.Stocks.Entities;

[Table("ItemStockSummary", Schema = "INV")]
public class ItemStockSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemStockSummaryID { get; set; }

    public int StockRef { get; set; }
    public int ItemRef { get; set; }
    public int? TracingRef { get; set; }

    [Column("Order")]
    public int Order { get; set; }

    public int UnitRef { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal InputQuantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal OutputQuantity { get; set; }

    // SQL Server computed column: InputQuantity - OutputQuantity.
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "decimal(19,4)")]
    public decimal Quantity { get; private set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? SaleQuantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? SaleWithReserveQuantity { get; set; }

    public int FiscalYearRef { get; set; }
    public bool FeedFromClosingOperation { get; set; }

    public Item Item { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
