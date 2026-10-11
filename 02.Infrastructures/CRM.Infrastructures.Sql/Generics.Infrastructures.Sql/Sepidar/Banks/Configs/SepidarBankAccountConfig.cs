using CRM.Core.Domain.Sepidar.Banks.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Banks.Configs;

public class SepidarBankAccountConfig : IEntityTypeConfiguration<SepidarBankAccount> {
    public void Configure(EntityTypeBuilder<SepidarBankAccount> builder)
    {
        builder.HasOne(f => f.Dl)
            .WithOne(f => f.SepidarBankAccount)
            .HasForeignKey<SepidarBankAccount>(f => f.DlRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}