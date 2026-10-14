using CRM.Core.Domain.ASettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.ASettings.Configs;

public class ExternalSoftwareConfigs : IEntityTypeConfiguration<ExternalSoftware> {
    public void Configure(EntityTypeBuilder<ExternalSoftware> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).ValueGeneratedOnAdd();

        builder.HasMany(x => x.BankAccountExternalSoftwares)
            .WithOne()
            .HasForeignKey(x => x.ExSoftwareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}