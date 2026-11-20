using CRM.Core.Domain.Sepidar.Purchases.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Purchases.Configs;

internal sealed class InventoryReceiptConfig : IEntityTypeConfiguration<InventoryReceipt>
{
    public void Configure(EntityTypeBuilder<InventoryReceipt> builder)
    {
        builder.ToTable("InventoryReceipt", "INV");
        builder.HasKey(x => x.InventoryReceiptID);
        builder.Property(x => x.InventoryReceiptID).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.TotalPrice).HasPrecision(19, 3);
        builder.Property(x => x.TotalTax).HasPrecision(19, 3);
        builder.Property(x => x.TotalDuty).HasPrecision(19, 3);
        builder.Property(x => x.TotalTransportPrice).HasPrecision(19, 3);
        builder.Property(x => x.TotalNetPrice).HasPrecision(19, 3);
        builder.Property(x => x.CreationDate).HasColumnType("datetime");
        builder.Property(x => x.LastModificationDate).HasColumnType("datetime");
        builder.Property(x => x.TotalReturnedPrice).HasPrecision(19, 4);
        builder.Property(x => x.TotalReturnedNetPrice).HasPrecision(19, 4);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.TotalOtherCost).HasPrecision(19, 3);

        builder.HasOne<InventoryPurchaseInvoice>().WithMany()
            .HasForeignKey(x => x.BasePurchaseInvoiceRef).OnDelete(DeleteBehavior.NoAction);
    }
}
