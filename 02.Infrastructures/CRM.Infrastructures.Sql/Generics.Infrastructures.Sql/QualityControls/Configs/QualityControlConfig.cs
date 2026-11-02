using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlConfig : IEntityTypeConfiguration<QualityControl>
{
    public void Configure(EntityTypeBuilder<QualityControl> builder)
    {
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.DealId).IsUnique();
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne(item => item.Deal).WithMany().HasForeignKey(item => item.DealId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Answers).WithOne(item => item.QualityControl)
            .HasForeignKey(item => item.QualityControlId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
