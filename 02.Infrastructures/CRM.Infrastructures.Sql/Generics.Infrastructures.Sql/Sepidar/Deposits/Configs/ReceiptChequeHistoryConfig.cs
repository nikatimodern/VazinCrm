using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptChequeHistoryConfig : IEntityTypeConfiguration<ReceiptChequeHistory>
{
    public void Configure(EntityTypeBuilder<ReceiptChequeHistory> builder)
    {
        builder.ToTable("ReceiptChequeHistory", "RPA");
        builder.HasKey(x => x.ReceiptChequeHistoryId);
        builder.Property(x => x.ReceiptChequeHistoryId).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");

        builder.HasOne<ReceiptCheque>().WithMany()
            .HasForeignKey(x => x.ReceiptChequeRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptChequeHistory>().WithMany()
            .HasForeignKey(x => x.ReceiptChequeHistoryRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptChequeBankingItem>().WithMany()
            .HasForeignKey(x => x.ReceiptChequeBankingItemRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<PaymentChequeOther>().WithMany()
            .HasForeignKey(x => x.PaymentChequeOtherRef).OnDelete(DeleteBehavior.NoAction);
    }
}
