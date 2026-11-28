using CRM.Core.Domain.Ai.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Ai.Configs;

internal sealed class AiTaskRouteConfig : IEntityTypeConfiguration<AiTaskRoute>
{
    public void Configure(EntityTypeBuilder<AiTaskRoute> builder)
    {
        builder.ToTable("AiTaskRoutes", table =>
        {
            table.HasCheckConstraint("CK_AiTaskRoutes_Timeout", "[TimeoutMs] BETWEEN 1 AND 300000");
            table.HasCheckConstraint("CK_AiTaskRoutes_OutputTokens", "[MaxOutputTokens] BETWEEN 1 AND 8192");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskKey).HasMaxLength(96).IsRequired();
        builder.HasIndex(x => x.TaskKey).IsUnique();
        builder.HasOne<AiModel>().WithMany().HasForeignKey(x => x.PrimaryModelId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
