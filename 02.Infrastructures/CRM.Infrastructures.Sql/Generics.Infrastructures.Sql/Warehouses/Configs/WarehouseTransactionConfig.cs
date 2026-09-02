using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseTransactionConfig : IEntityTypeConfiguration<WarehouseTransaction> {
    public void Configure(EntityTypeBuilder<WarehouseTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Weight).HasPrecision(20, 0);

        builder.HasOne(x => x.WarehouseProduct)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WarehouseProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}