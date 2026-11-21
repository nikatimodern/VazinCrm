using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class PaymentChequeOtherConfig : IEntityTypeConfiguration<PaymentChequeOther>
{
    public void Configure(EntityTypeBuilder<PaymentChequeOther> builder)
    {
        builder.ToTable("PaymentChequeOther", "RPA");
        builder.HasKey(x => x.PaymentChequeOtherId);
        builder.Property(x => x.PaymentChequeOtherId).ValueGeneratedNever();
        builder.Property(x => x.HeaderDate).HasColumnType("datetime");
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Description_En).HasMaxLength(4000);
        builder.HasOne<PaymentHeader>().WithMany().HasForeignKey(x => x.PaymentHeaderRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptCheque>().WithMany().HasForeignKey(x => x.ReceiptChequeRef).OnDelete(DeleteBehavior.NoAction);
    }
}
