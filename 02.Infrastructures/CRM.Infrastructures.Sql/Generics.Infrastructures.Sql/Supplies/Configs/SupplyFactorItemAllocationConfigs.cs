using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Supplies.Configs;

public class SupplyFactorItemAllocationConfigs : IEntityTypeConfiguration<SupplyFactorItemAllocation> {
    public void Configure(EntityTypeBuilder<SupplyFactorItemAllocation> builder) {
        builder.ToTable("SupplyFactorItemAllocations", t => {
            t.HasCheckConstraint("CK_SupplyFactorItemAllocations_Weight", "[Weight] > 0");
            t.HasCheckConstraint("CK_SupplyFactorItemAllocations_Destination",
                "([FactorItemId] IS NOT NULL AND [WarehouseId] IS NULL) OR ([FactorItemId] IS NULL AND [WarehouseId] IS NOT NULL)");
        });
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Weight).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.SupplyFactorItemId, x.FactorItemId });
        builder.HasOne(x => x.SupplyFactorItem).WithMany(x => x.Allocations)
            .HasForeignKey(x => x.SupplyFactorItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FactorItem).WithMany(x => x.Allocations)
            .HasForeignKey(x => x.FactorItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
