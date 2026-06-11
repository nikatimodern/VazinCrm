using CRM.Core.Domain.Checks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Checks.Configs;

public class CheckConfig : IEntityTypeConfiguration<Check> {
    public void Configure(EntityTypeBuilder<Check> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(t => t.Amount).HasPrecision(20, 0);

        builder.HasOne(f => f.Checkbook)
            .WithMany(c => c.Checks)
            .HasForeignKey(f => f.CheckbookId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}