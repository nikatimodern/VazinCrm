using CRM.Core.Domain.Checks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Checks.Configs;

public class CheckbookConfig :IEntityTypeConfiguration<Checkbook> {
    public void Configure(EntityTypeBuilder<Checkbook> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasOne(f => f.BankAccount)
    }
}