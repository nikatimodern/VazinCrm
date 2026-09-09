using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseVoucherConfig : IEntityTypeConfiguration<WarehouseVoucher> {
    public void Configure(EntityTypeBuilder<WarehouseVoucher> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VoucherNumber).ValueGeneratedOnAdd();
        
        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseVouchers)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}