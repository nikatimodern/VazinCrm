using CRM.Core.Domain.PreFactors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.PreFactors.Configs
{
    public class PreFactorConfig : IEntityTypeConfiguration<PreFactor>
    {
        public void Configure(EntityTypeBuilder<PreFactor> builder)
        {
            builder.HasKey(f => f.Id);


            builder.HasMany(f => f.PreFactorItems)
                .WithOne()
                .HasForeignKey(f => f.PrefactorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Descriptions)
                .WithOne()
                .HasForeignKey(f => f.PrefactorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Factors)
                .WithOne()
                .HasForeignKey(f => f.PfactorID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Deal)
                .WithMany(x => x.PreFactors)
                .HasForeignKey(x => x.DealId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(f => f.PaymentAmount).HasPrecision(20, 0);
            builder.Property(f => f.Discount).HasPrecision(20, 0);
            builder.Property(f => f.TotalTax).HasPrecision(20, 0);
            builder.Property(f => f.SumTotalAmount).HasPrecision(20, 0);
            builder.Property(f => f.SumTotalAmountTax).HasPrecision(20, 0);
            builder.Property(f => f.ReciveAmount).HasPrecision(20, 0);

            //builder.Property(u => u.SerialNumber).ValueGeneratedOnAdd().IsRequired();
            //builder.Property(u => u.NextSerialNumber).ValueGeneratedOnAdd();
        }
    }
}
