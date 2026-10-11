using CRM.Core.Domain.Sepidar.Banks.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Banks.Configs;

public class SepidarBankConfig : IEntityTypeConfiguration<SepidarBank> {


    public void Configure(EntityTypeBuilder<SepidarBank> builder)
    {
        builder.HasMany(f => f.SepidarBankBranches)
            .WithOne(f => f.SepidarBank)
            .HasForeignKey(f => f.BankRef)
            .OnDelete(DeleteBehavior.Cascade);
    }
}