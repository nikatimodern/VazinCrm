using CRM.Core.Domain.ExternalSoftwares.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.ExternalSoftwares.Configs;

public class ExternalSoftwareConfigs : IEntityTypeConfiguration<ExternalSoftware> {
    public void Configure(EntityTypeBuilder<ExternalSoftware> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).ValueGeneratedOnAdd();

        builder.HasMany(x => x.ExternalSoftwareSettings)
            .WithOne()
            .HasForeignKey(x => x.ExternalSoftwareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}