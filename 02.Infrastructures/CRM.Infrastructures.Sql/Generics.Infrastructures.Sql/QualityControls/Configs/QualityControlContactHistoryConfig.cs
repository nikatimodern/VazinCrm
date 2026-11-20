using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlContactHistoryConfig : IEntityTypeConfiguration<QualityControlContactHistory>
{
    public void Configure(EntityTypeBuilder<QualityControlContactHistory> builder)
    {
        builder.ToTable("QualityControlContactHistories", table =>
        {
            table.HasCheckConstraint("CK_QcContactHistories_Status", "[Status] IN (0, 1, 2)");
            table.HasCheckConstraint("CK_QcContactHistories_Attempts", "[AttemptCount] >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.CustomerId, item.CreateDate });
        builder.Property(item => item.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(item => item.AttemptCount).HasDefaultValue(0);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.Property(item => item.MobileAtAttempt).HasMaxLength(50);
        builder.Property(item => item.Description).HasMaxLength(500);
        builder.Property(item => item.DeleteReason).HasMaxLength(500);
        builder.Property(item => item.DeletedById).HasMaxLength(450);
        builder.Property(item => item.DeletedByName).HasMaxLength(250);
        builder.Property(item => item.RestoredById).HasMaxLength(450);
        builder.Property(item => item.RestoredByName).HasMaxLength(250);
        builder.HasOne<Customer>().WithMany().HasForeignKey(item => item.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
