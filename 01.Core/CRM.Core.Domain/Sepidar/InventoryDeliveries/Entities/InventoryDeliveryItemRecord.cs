using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Sepidar.Invoices.Entities;

namespace CRM.Core.Domain.Sepidar.InventoryDeliveries.Entities;

[Table("InventoryDeliveryItem", Schema = "INV")]
public class InventoryDeliveryItemRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int InventoryDeliveryItemID { get; set; }

    public int InventoryDeliveryRef { get; set; }
    public bool IsReturn { get; set; }
    public int RowNumber { get; set; }
    public int? BaseInvoiceItem { get; set; }
    public int? BaseInventoryDeliveryItem { get; set; }
    public int? BaseReturnedInvoiceItem { get; set; }
    public int? QuotationItemRef { get; set; }
    public int ItemRef { get; set; }
    public int? TracingRef { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? SecondaryQuantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal RemainingQuantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? RemainingSecondaryQuantity { get; set; }

    public int? SLAccountRef { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? Price { get; set; }

    public string? Description { get; set; }
    public string? Description_En { get; set; }
    public int Version { get; set; }
    public int? ProductOrderRef { get; set; }
    public string? ParityCheck { get; set; }
    public int? WeighingRef { get; set; }
    public int? ItemRequestItemRef { get; set; }
    public string? ItemDescription { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? Fee { get; private set; }

    public InventoryDeliveryRecord InventoryDelivery { get; set; } = null!;
    public SalesInvoiceItemRecord? InvoiceItem { get; set; }
}
