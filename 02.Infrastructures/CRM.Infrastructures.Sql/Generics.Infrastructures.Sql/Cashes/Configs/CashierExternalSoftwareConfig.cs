using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class CashierExternalSoftwareConfig : IEntityTypeConfiguration<CashierExternalSoftware> {
    public void Configure(EntityTypeBuilder<CashierExternalSoftware> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.ExternalSoftware)
            .WithMany(f => f.CashierMappings)
            .HasForeignKey(f => f.ExternalSoftwareId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}