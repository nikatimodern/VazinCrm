using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseUserConfig : IEntityTypeConfiguration<WarehouseUser> {
    public void Configure(EntityTypeBuilder<WarehouseUser> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        
        builder.HasKey(x => new { x.UserId, x.WarehouseId });

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseUsers)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}