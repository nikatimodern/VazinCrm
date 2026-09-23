using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Supplies.Configs;

public class SupplyFactorConfigs : IEntityTypeConfiguration<SupplyFactor> {
    public void Configure(EntityTypeBuilder<SupplyFactor> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasMany(f => f.SupplyFactorOrderHeads)
            .WithOne()
            .HasForeignKey(f => f.SupplyFactorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.RequestAmountSupplyFactor)
            .WithOne(f => f.SupplyFactor)
            .HasForeignKey(f => f.SupplyFactorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.PaymentSupplyFactors)
            .WithOne()
            .HasForeignKey(f => f.SupplyFactorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Deal)
            .WithMany(f => f.SupplyFactors)
            .HasForeignKey(f => f.DealId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(f => f.PaymentAmount).HasPrecision(20, 0);
        builder.Property(f => f.SumTotalAmount).HasPrecision(20, 0);
        builder.Property(f => f.TotalTax).HasPrecision(20, 0);
        builder.Property(f => f.Discount).HasPrecision(20, 0);
    }
}