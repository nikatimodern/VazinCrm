using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseProductConfig : IEntityTypeConfiguration<WarehouseProduct> {
    public void Configure(EntityTypeBuilder<WarehouseProduct> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.SupplyFactorItem).WithMany().HasForeignKey(x => x.SupplyFactorItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WarehouseId, x.SupplyFactorItemId }).IsUnique()
            .HasFilter("[SupplyFactorItemId] IS NOT NULL");

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseProducts)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Product)
            .WithMany(x => x.WarehouseProducts)
            .HasForeignKey(x => x.ProductCode)
            .OnDelete(DeleteBehavior.SetNull);
    }
}