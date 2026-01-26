using CRM.Core.Domain.Factors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Factors.Configs;

public class FactorItemConfig : IEntityTypeConfiguration<FactorItem> {
    public void Configure(EntityTypeBuilder<FactorItem> builder) {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.UnitAmount).HasPrecision(20, 0);
        builder.Property(f => f.TotalAmount).HasPrecision(20, 0);
    }
}