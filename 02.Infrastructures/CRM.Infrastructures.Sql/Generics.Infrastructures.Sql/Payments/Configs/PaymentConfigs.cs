using CRM.Core.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Payments.Configs {
    public class PaymentConfigs : IEntityTypeConfiguration<Payment> {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(x => x.PaymentNumber).ValueGeneratedOnAdd();

            builder.Property(f => f.Amount).HasPrecision(20, 0);

            builder.HasMany(f => f.PaymentSupplyFactors)
                .WithOne()
                .HasForeignKey(f => f.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.RequestAmountPayments)
                .WithOne(f => f.Payment)
                .HasForeignKey(f => f.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}