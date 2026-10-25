using CRM.Core.Domain.Sepidar.Stocks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Stocks.Configs;

public class StockConfigs : IEntityTypeConfiguration<Stock> {
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.HasMany(x => x.ItemStocks)
            .WithOne(x => x.Stock)
            .HasForeignKey(x => x.StockRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}