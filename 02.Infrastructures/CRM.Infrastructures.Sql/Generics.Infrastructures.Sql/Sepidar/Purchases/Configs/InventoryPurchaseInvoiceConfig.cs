using CRM.Core.Domain.Sepidar.Purchases.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Purchases.Configs;

internal sealed class InventoryPurchaseInvoiceConfig : IEntityTypeConfiguration<InventoryPurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<InventoryPurchaseInvoice> builder)
    {
        builder.ToTable("InventoryPurchaseInvoice", "INV");
        builder.HasKey(x => x.InventoryPurchaseInvoiceID);
        builder.Property(x => x.InventoryPurchaseInvoiceID).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.CurrencyRate).HasPrecision(26, 16);
        builder.Property(x => x.TotalPrice).HasPrecision(19, 4);
        builder.Property(x => x.TotalPriceInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.TotalTax).HasPrecision(19, 4);
        builder.Property(x => x.TotalDuty).HasPrecision(19, 4);
        builder.Property(x => x.TotalTransportPrice).HasPrecision(19, 4);
        builder.Property(x => x.TotalNetPrice).HasPrecision(19, 4);
        builder.Property(x => x.TotalNetPriceInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.TotalWithHoldingTaxAmount).HasPrecision(19, 4);
        builder.Property(x => x.TotalInsuranceAmount).HasPrecision(19, 4);
        builder.Property(x => x.CreationDate).HasColumnType("datetime");
        builder.Property(x => x.LastModificationDate).HasColumnType("datetime");
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.Description_En).HasMaxLength(400);
    }
}
