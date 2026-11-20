using CRM.Core.Domain.Sepidar.Purchases.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Purchases.Configs;

internal sealed class InventoryReceiptItemConfig : IEntityTypeConfiguration<InventoryReceiptItem>
{
    public void Configure(EntityTypeBuilder<InventoryReceiptItem> builder)
    {
        builder.ToTable("InventoryReceiptItem", "INV");
        builder.HasKey(x => x.InventoryReceiptItemID);
        builder.Property(x => x.InventoryReceiptItemID).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(19, 4);
        builder.Property(x => x.SecondaryQuantity).HasPrecision(19, 4);
        builder.Property(x => x.RemainingQuantity).HasPrecision(19, 4);
        builder.Property(x => x.RemainingSecondaryQuantity).HasPrecision(19, 4);
        builder.Property(x => x.CurrencyRate).HasPrecision(26, 16);
        builder.Property(x => x.CurrencyValue).HasPrecision(19, 4);
        builder.Property(x => x.Price).HasPrecision(19, 4);
        builder.Property(x => x.Tax).HasPrecision(19, 4);
        builder.Property(x => x.Duty).HasPrecision(19, 4);
        builder.Property(x => x.TransportPrice).HasPrecision(19, 4);
        builder.Property(x => x.TransportTax).HasPrecision(19, 4);
        builder.Property(x => x.TransportDuty).HasPrecision(19, 4);
        builder.Property(x => x.TransportDescription).HasMaxLength(250);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Description_En).HasMaxLength(4000);
        builder.Property(x => x.ParityCheck).HasMaxLength(250);
        builder.Property(x => x.TaxCurrencyValue).HasPrecision(19, 4);
        builder.Property(x => x.DutyCurrencyValue).HasPrecision(19, 4);
        builder.Property(x => x.ReturnedPrice).HasPrecision(19, 4);
        builder.Property(x => x.Fee).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.ReturnedFee).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.AllotmenedOtherCostInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.NetPrice).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.ReturnedNetPrice).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();

        builder.HasOne<InventoryReceipt>().WithMany()
            .HasForeignKey(x => x.InventoryReceiptRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<InventoryPurchaseInvoiceItem>().WithMany()
            .HasForeignKey(x => x.BasePurchaseInvoiceItemRef).OnDelete(DeleteBehavior.NoAction);
    }
}
