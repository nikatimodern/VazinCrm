using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs
{
    public class CashConfigs : IEntityTypeConfiguration<Cash>
    {
        public void Configure(EntityTypeBuilder<Cash> builder)
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Amount).HasPrecision(20, 0);
            builder.Property(f => f.Fee).HasPrecision(20, 0);
            builder.Property(x => x.Serial).ValueGeneratedOnAdd();

            builder.HasOne(f => f.Accountancy)
                .WithOne()
                .HasForeignKey<Accountancy>(f => f.CashId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.BankAccount)
                .WithMany(f => f.Cashes)
                .HasForeignKey(f => f.BankAccountId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

}
