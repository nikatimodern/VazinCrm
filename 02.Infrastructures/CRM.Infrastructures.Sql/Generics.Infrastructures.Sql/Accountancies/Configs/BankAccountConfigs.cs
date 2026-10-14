using CRM.Core.Domain.Accountancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Accountancies.Configs;

public class BankAccountConfigs : IEntityTypeConfiguration<BankAccount> {
    public void Configure(EntityTypeBuilder<BankAccount> builder) {
        builder.HasKey(f => f.Id);

        builder.HasMany(f => f.Accountancies)
            .WithOne()
            .HasForeignKey(f => f.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
    }
}