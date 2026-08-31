using CRM.Core.Domain.Warehouses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Warehouses.Configs;

public class WarehouseProductConfig : IEntityTypeConfiguration<WarehouseProduct> {
    public void Configure(EntityTypeBuilder<WarehouseProduct> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.WarehouseProducts)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Weight).HasPrecision(20, 0);
    }
}