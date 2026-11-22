using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptChequeConfig : IEntityTypeConfiguration<ReceiptCheque>
{
    public void Configure(EntityTypeBuilder<ReceiptCheque> builder)
    {
        builder.ToTable("ReceiptCheque", "RPA");
        builder.HasKey(x => x.ReceiptChequeId);
        builder.Property(x => x.ReceiptChequeId).ValueGeneratedNever();
        builder.Property(x => x.HeaderDate).HasColumnType("datetime");
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.Number).HasMaxLength(50);
        builder.Property(x => x.SecondNumber).HasMaxLength(50);
        builder.Property(x => x.ChequeOwner).HasMaxLength(250);
        builder.Property(x => x.BranchCode).HasMaxLength(250);
        builder.Property(x => x.BranchTitle).HasMaxLength(250);
        builder.Property(x => x.AccountNo).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Description_En).HasMaxLength(4000);
        builder.Property(x => x.SayadCode).HasColumnType("char(20)");
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.Property(x => x.Rate).HasPrecision(26, 16);
        builder.Property(x => x.AmountInBaseCurrency).HasPrecision(19, 4);

        builder.HasOne<ReceiptHeader>().WithMany()
            .HasForeignKey(x => x.ReceiptHeaderRef).OnDelete(DeleteBehavior.NoAction);
    }
}
