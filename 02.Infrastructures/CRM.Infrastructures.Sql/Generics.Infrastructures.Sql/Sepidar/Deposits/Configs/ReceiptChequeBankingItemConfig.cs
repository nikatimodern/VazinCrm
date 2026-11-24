using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptChequeBankingItemConfig : IEntityTypeConfiguration<ReceiptChequeBankingItem>
{
    public void Configure(EntityTypeBuilder<ReceiptChequeBankingItem> builder)
    {
        builder.ToTable("ReceiptChequeBankingItem", "RPA");
        builder.HasKey(x => x.ReceiptChequeBankingItemId);
        builder.Property(x => x.ReceiptChequeBankingItemId).ValueGeneratedNever();
        builder.Property(x => x.HeaderDate).HasColumnType("datetime");
        builder.Property(x => x.ForcastDate).HasColumnType("datetime");
        builder.HasOne<ReceiptChequeBanking>().WithMany().HasForeignKey(x => x.ReceiptChequeBankingRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptCheque>().WithMany().HasForeignKey(x => x.ReceiptChequeRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptChequeBankingItem>().WithMany().HasForeignKey(x => x.ReceiptChequeBankingItemRef).OnDelete(DeleteBehavior.NoAction);
    }
}
