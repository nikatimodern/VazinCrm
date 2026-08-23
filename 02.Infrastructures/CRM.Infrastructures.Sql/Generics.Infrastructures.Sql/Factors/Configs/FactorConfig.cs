using CRM.Core.Domain.Factors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Factors.Configs
{
    public class FactorConfig : IEntityTypeConfiguration<Factor>
    {
        public void Configure(EntityTypeBuilder<Factor> builder)
        {

            builder.HasKey(f => f.Id);

            builder.HasMany(f => f.FactorItems)
                .WithOne()
                .HasForeignKey(f => f.factorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Descriptions)
                .WithOne()
                .HasForeignKey(f => f.factorId)
                .OnDelete(DeleteBehavior.Cascade);



            builder.Property(f => f.PaymentAmount).HasPrecision(20, 0);
            builder.Property(f => f.Discount).HasPrecision(20, 0);
            builder.Property(f => f.TotalTax).HasPrecision(20, 0);
            builder.Property(f => f.SumTotalAmount).HasPrecision(20, 0);
            builder.Property(f => f.SumTotalAmountTax).HasPrecision(20, 0);
            builder.Property(f => f.ReciveAmount).HasPrecision(20, 0);
            
            builder.HasOne(x => x.Deal)
                .WithMany(x => x.Factors)
                .HasForeignKey(x => x.DealId)
                .OnDelete(DeleteBehavior.NoAction);
            //builder.Property(f => f.SerialNumber).ValueGeneratedOnAdd();

        }
    }
}
