using CRM.Core.Domain.RequestAmounts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CRM.Core.Domain.Accountancies.Entities;

namespace CRM.Infrastructures.Sql.RequestAmounts.Configs {
    public class RequestAmountConfigs : IEntityTypeConfiguration<RequestAmount> {
        public void Configure(EntityTypeBuilder<RequestAmount> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RequestNumber).ValueGeneratedOnAdd();


            builder.HasMany(f => f.RequestAmountSupplyFactor)
                .WithOne(f => f.RequestAmount)
                .HasForeignKey(f => f.RequestAmountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.RequestAmountOrderHeads)
                .WithOne(f => f.RequestAmount)
                .HasForeignKey(f => f.RequestAmountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.RequestAmountPayments)
                .WithOne(f => f.RequestAmount)
                .HasForeignKey(f => f.RequestAmountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Accountancy)
                .WithOne()
                .HasForeignKey<Accountancy>(f => f.RequestAmountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(f => f.Amount).HasPrecision(20, 0);
            builder.Property(f => f.AcceptedAmount).HasPrecision(20, 0);
        }
    }
}