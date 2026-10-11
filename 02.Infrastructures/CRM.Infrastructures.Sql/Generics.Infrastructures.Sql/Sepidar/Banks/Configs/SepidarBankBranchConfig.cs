using CRM.Core.Domain.Sepidar.Banks.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Banks.Configs;

public class SepidarBankBranchConfig : IEntityTypeConfiguration<SepidarBankBranch> {
    public void Configure(EntityTypeBuilder<SepidarBankBranch> builder)
    {
        builder.HasMany(f => f.SepidarBankAccounts)
            .WithOne(f => f.SepidarBankBranch)
            .HasForeignKey(f => f.BankBranchRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}