using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseTransactionConfig : IEntityTypeConfiguration<WarehouseTransaction> {
    public void Configure(EntityTypeBuilder<WarehouseTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseTransactions)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Weight).HasPrecision(20, 0);
    }
}