using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Sepidar.InventoryDeliveries.Entities;

[Table("InventoryDelivery", Schema = "INV")]
public class InventoryDeliveryRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int InventoryDeliveryID { get; set; }

    public bool IsReturn { get; set; }
    public int Type { get; set; }
    public int StockRef { get; set; }
    public int? ReceiverDLRef { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? TotalPrice { get; set; }

    public int? AccountingVoucherRef { get; set; }
    public int FiscalYearRef { get; set; }
    public int? DestinationStockRef { get; set; }
    public int CreatorForm { get; set; }
    public int Creator { get; set; }
    public DateTime CreationDate { get; set; }
    public int LastModifier { get; set; }
    public DateTime LastModificationDate { get; set; }
    public int Version { get; set; }
    public string? Description { get; set; }

    public IList<InventoryDeliveryItemRecord> Items { get; set; } = new List<InventoryDeliveryItemRecord>();
}
