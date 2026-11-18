using CRM.Core.Domain.Sepidar.Purchases.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Purchases.Configs;

internal sealed class InventoryPurchaseInvoiceItemConfig : IEntityTypeConfiguration<InventoryPurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<InventoryPurchaseInvoiceItem> builder)
    {
        builder.ToTable("InventoryPurchaseInvoiceItem", "INV");
        builder.HasKey(x => x.InventoryPurchaseInvoiceItemID);
        builder.Property(x => x.InventoryPurchaseInvoiceItemID).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(19, 4);
        builder.Property(x => x.SecondaryQuantity).HasPrecision(19, 4);
        builder.Property(x => x.RemainingQuantity).HasPrecision(19, 4);
        builder.Property(x => x.RemainingSecondaryQuantity).HasPrecision(19, 4);
        builder.Property(x => x.CurrencyRate).HasPrecision(26, 16);
        builder.Property(x => x.Fee).HasPrecision(19, 4);
        builder.Property(x => x.FeeInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Price).HasPrecision(19, 4);
        builder.Property(x => x.PriceInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Discount).HasPrecision(19, 4);
        builder.Property(x => x.DiscountInBaseCurrentcy).HasPrecision(19, 4);
        builder.Property(x => x.Addition).HasPrecision(19, 4);
        builder.Property(x => x.AdditionInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Tax).HasPrecision(19, 4);
        builder.Property(x => x.TaxInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Duty).HasPrecision(19, 4);
        builder.Property(x => x.DutyInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.TransportPrice).HasPrecision(19, 4);
        builder.Property(x => x.TransportPriceInBaseCurrency).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.TransportTax).HasPrecision(19, 4);
        builder.Property(x => x.TransportTaxInBaseCurrency).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.TransportDuty).HasPrecision(19, 4);
        builder.Property(x => x.TransportDutyInBaseCurrency).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.TransportDescription).HasMaxLength(250);
        builder.Property(x => x.NetPriceInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Description_En).HasMaxLength(500);
        builder.Property(x => x.InsuranceAmount).HasPrecision(19, 4);
        builder.Property(x => x.InsuranceAmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.WithHoldingTaxAmount).HasPrecision(19, 4);
        builder.Property(x => x.WithHoldingTaxAmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.NetPrice).HasPrecision(19, 4).ValueGeneratedOnAddOrUpdate();

        builder.HasOne<InventoryPurchaseInvoice>().WithMany()
            .HasForeignKey(x => x.InventoryPurchaseInvoiceRef).OnDelete(DeleteBehavior.NoAction);
    }
}
