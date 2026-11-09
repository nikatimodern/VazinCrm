using CRM.Core.Domain.Sepidar.InventoryDeliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.InventoryDeliveries.Configs;

public sealed class InventoryDeliveryRecordConfigs :
    IEntityTypeConfiguration<InventoryDeliveryRecord>,
    IEntityTypeConfiguration<InventoryDeliveryItemRecord>
{
    public void Configure(EntityTypeBuilder<InventoryDeliveryRecord> builder)
    {
        builder.ToTable("InventoryDelivery", "INV");
        builder.HasKey(x => x.InventoryDeliveryID);
        builder.Property(x => x.InventoryDeliveryID).ValueGeneratedNever();

        builder.HasMany(x => x.Items)
            .WithOne(x => x.InventoryDelivery)
            .HasForeignKey(x => x.InventoryDeliveryRef)
            .OnDelete(DeleteBehavior.NoAction);
    }

    public void Configure(EntityTypeBuilder<InventoryDeliveryItemRecord> builder)
    {
        builder.ToTable("InventoryDeliveryItem", "INV");
        builder.HasKey(x => x.InventoryDeliveryItemID);
        builder.Property(x => x.InventoryDeliveryItemID).ValueGeneratedNever();
        builder.Property(x => x.Fee).ValueGeneratedOnAddOrUpdate();

        builder.HasOne(x => x.InvoiceItem)
            .WithMany()
            .HasForeignKey(x => x.BaseInvoiceItem)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
