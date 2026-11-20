using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlItemAnswerConfig : IEntityTypeConfiguration<QualityControlItemAnswer>
{
    public void Configure(EntityTypeBuilder<QualityControlItemAnswer> builder)
    {
        builder.ToTable("QualityControlItemAnswers",
            table => table.HasCheckConstraint("CK_QualityControlItemAnswers_Answer", "[Answer] IN (0, 1)"));
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.QualityControlItemReviewId, item.QuestionId }).IsUnique();
        builder.Property(item => item.QuestionTitleSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(item => item.QuestionTextSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.ZeroAnswerTextSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(item => item.OneAnswerTextSnapshot).HasMaxLength(250).IsRequired();
        builder.HasOne(item => item.Question).WithMany().HasForeignKey(item => item.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
