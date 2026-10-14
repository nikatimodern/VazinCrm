using CRM.Core.Domain.ASettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.ASettings.Configs;

public class ExternalMappingConfigs : IEntityTypeConfiguration<ExternalMapping> {
    public void Configure(EntityTypeBuilder<ExternalMapping> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ExternalSoftware)
            .WithMany(x => x.ExternalMappings)
            .HasForeignKey(x => x.ExternalSoftwareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}