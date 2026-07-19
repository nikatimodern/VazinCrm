using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class CashGroupConfig: IEntityTypeConfiguration<CashGroup> {
    public void Configure(EntityTypeBuilder<CashGroup> builder)
    {
        builder.HasKey(x => x.Id);


        builder.Property(x => x.GroupNumber)
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => x.GroupNumber)
            .IsUnique();

        builder.HasMany(x => x.Cashes)
            .WithOne(x => x.CashGroup)
            .HasForeignKey(x => x.CashGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}