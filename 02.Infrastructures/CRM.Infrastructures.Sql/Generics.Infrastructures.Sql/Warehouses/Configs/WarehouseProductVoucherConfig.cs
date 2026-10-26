using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseProductVoucherConfig : IEntityTypeConfiguration<WarehouseProductVoucher> {
    public void Configure(EntityTypeBuilder<WarehouseProductVoucher> builder)
    {
        builder.HasKey(f => new { f.WarehouseProductId, f.WarehouseVoucherId });
        
        builder.Property(x => x.Weight).HasPrecision(20, 2);
        
        builder.HasOne(f => f.WarehouseVoucher)
            .WithMany(f => f.WarehouseProductVouchers)
            .HasForeignKey(f => f.WarehouseVoucherId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(f => f.WarehouseProduct)
            .WithMany(f => f.WarehouseProductVouchers)
            .HasForeignKey(f => f.WarehouseProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}