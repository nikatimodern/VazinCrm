using CRM.Core.Domain.Sepidar.Parties.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Parties.Configs;

public class PartyConfigs : IEntityTypeConfiguration<Party> {
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.HasOne(f => f.Dl)
            .WithOne()
            .HasForeignKey<Party>(f => f.DLRef)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(f => f.PartyAddresses)
            .WithOne()
            .HasForeignKey(f => f.PartyRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}