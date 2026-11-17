using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlItemReviewConfig : IEntityTypeConfiguration<QualityControlItemReview>
{
    public void Configure(EntityTypeBuilder<QualityControlItemReview> builder)
    {
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.FactorItemId).IsUnique();
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.ToTable("QualityControlItemReviews",
            table => table.HasCheckConstraint("CK_QualityControlItemReviews_Status", "[Status] IN (0, 1, 2)"));
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne(item => item.FactorItem).WithMany().HasForeignKey(item => item.FactorItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Answers).WithOne(item => item.QualityControlItemReview)
            .HasForeignKey(item => item.QualityControlItemReviewId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
