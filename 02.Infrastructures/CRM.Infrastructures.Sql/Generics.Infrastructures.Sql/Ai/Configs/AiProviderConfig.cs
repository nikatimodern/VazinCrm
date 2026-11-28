using CRM.Core.Domain.Ai.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Ai.Configs;

internal sealed class AiProviderConfig : IEntityTypeConfiguration<AiProvider>
{
    public void Configure(EntityTypeBuilder<AiProvider> builder)
    {
        builder.ToTable("AiProviders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EndpointRef).HasMaxLength(128);
        builder.Property(x => x.SecretRef).HasMaxLength(128);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
