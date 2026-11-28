using CRM.Core.Domain.Ai.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Ai.Configs;

internal sealed class AiModelConfig : IEntityTypeConfiguration<AiModel>
{
    public void Configure(EntityTypeBuilder<AiModel> builder)
    {
        builder.ToTable("AiModels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModelKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.ProviderId, x.ModelKey }).IsUnique();
        builder.HasOne<AiProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
