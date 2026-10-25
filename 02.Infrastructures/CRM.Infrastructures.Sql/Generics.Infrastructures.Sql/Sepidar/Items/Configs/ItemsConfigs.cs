using CRM.Core.Domain.Sepidar.Items.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Items.Configs;

public class ItemsConfigs : IEntityTypeConfiguration<Item> {
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasMany(x => x.ItemStocks)
            .WithOne(x => x.Item)
            .HasForeignKey(x => x.ItemRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}