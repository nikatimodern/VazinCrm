using CRM.Core.Domain.Sepidar.Payments.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Payments.Configs;

public class PaymentHeaderConfig : IEntityTypeConfiguration<PaymentHeader> {
    public void Configure(EntityTypeBuilder<PaymentHeader> builder)
    {
       
        builder.HasMany(f => f.PaymentDrafts)
            .WithOne()
            .HasForeignKey(f => f.PaymentHeaderRef)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(f => f.Voucher)
            .WithOne()
            .HasForeignKey<PaymentHeader>(f => f.VoucherRef)
            .OnDelete(DeleteBehavior.NoAction);
    }
}