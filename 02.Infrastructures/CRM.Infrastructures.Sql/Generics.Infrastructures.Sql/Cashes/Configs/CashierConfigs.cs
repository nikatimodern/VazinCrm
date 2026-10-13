using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs {
    public class CashierConfigs : IEntityTypeConfiguration<Cashier> {
        public void Configure(EntityTypeBuilder<Cashier> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasOne(f => f.BankAccount)
                .WithMany(f => f.Cashiers)
                .HasForeignKey(f => f.BankAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(f => f.CashierExternalSoftwares)
                .WithOne(f => f.Cashier)
                .HasForeignKey(f => f.CashierId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(f => f.Cashes)
                .WithOne(f => f.Cashier)
                .HasForeignKey(f => f.CashierId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}