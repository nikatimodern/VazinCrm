using CRM.Core.Domain.Sepidar.Stocks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Stocks.Configs;

public sealed class ItemStockSummaryConfigs : IEntityTypeConfiguration<ItemStockSummary>
{
    public void Configure(EntityTypeBuilder<ItemStockSummary> builder)
    {
        builder.ToTable("ItemStockSummary", "INV");
        builder.HasKey(x => x.ItemStockSummaryID);
        builder.Property(x => x.ItemStockSummaryID).ValueGeneratedNever();
        builder.Property(x => x.Quantity).ValueGeneratedOnAddOrUpdate();

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemRef)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Stock)
            .WithMany()
            .HasForeignKey(x => x.StockRef)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
