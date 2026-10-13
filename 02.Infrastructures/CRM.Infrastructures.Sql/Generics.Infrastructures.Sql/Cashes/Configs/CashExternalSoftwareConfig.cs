using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class CashExternalSoftwareConfig : IEntityTypeConfiguration<CashExternalSoftware> {
    public void Configure(EntityTypeBuilder<CashExternalSoftware> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.ExternalSoftware)
            .WithMany(f => f.CashExternalSoftwares)
            .HasForeignKey(f => f.ExternalSoftwareId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}