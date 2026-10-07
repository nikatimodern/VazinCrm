using CRM.Core.Domain.Accountancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Accountancies.Configs;

public class BankAccountExternalSoftwareConfig : IEntityTypeConfiguration<BankAccountExternalSoftware> {
    public void Configure(EntityTypeBuilder<BankAccountExternalSoftware> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.BankAccount)
            .WithMany(x => x.ExternalSoftwares)
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}