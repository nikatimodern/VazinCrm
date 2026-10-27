using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseTransactionConfig : IEntityTypeConfiguration<WarehouseTransaction> {
    public void Configure(EntityTypeBuilder<WarehouseTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.FactorItem).WithMany().HasForeignKey(x => x.FactorItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SupplyFactorItem).WithMany().HasForeignKey(x => x.SupplyFactorItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Weight).HasPrecision(20, 2);

        builder.HasOne(x => x.WarehouseProduct)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WarehouseProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.WarehouseVoucher)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WarehouseVoucherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}