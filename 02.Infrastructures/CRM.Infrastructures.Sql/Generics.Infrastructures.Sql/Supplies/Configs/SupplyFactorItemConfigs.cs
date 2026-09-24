using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Supplies.Configs;

public class SupplyFactorItemConfigs : IEntityTypeConfiguration<SupplyFactorItem> {
    public void Configure(EntityTypeBuilder<SupplyFactorItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SupplyFactor)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SupplyFactorId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.Property(f => f.TaxAmount).HasPrecision(20, 0);
        builder.Property(f => f.TotalAmount).HasPrecision(20, 0);
        builder.Property(f => f.UnitAmount).HasPrecision(20, 0);
        builder.Property(f => f.Discount).HasPrecision(20, 0);
    }
}