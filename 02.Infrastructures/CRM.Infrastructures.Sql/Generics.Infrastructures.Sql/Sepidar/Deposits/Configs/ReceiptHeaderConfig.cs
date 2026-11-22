using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptHeaderConfig : IEntityTypeConfiguration<ReceiptHeader>
{
    public void Configure(EntityTypeBuilder<ReceiptHeader> builder)
    {
        builder.ToTable("ReceiptHeader", "RPA");
        builder.HasKey(x => x.ReceiptHeaderId);
        builder.Property(x => x.ReceiptHeaderId).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.CreationDate).HasColumnType("datetime");
        builder.Property(x => x.LastModificationDate).HasColumnType("datetime");
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.CreatorForm).HasMaxLength(50);
        builder.Property(x => x.Description_En).HasMaxLength(4000);
        builder.Property(x => x.Discount).HasPrecision(19, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(19, 4);
        builder.Property(x => x.Rate).HasPrecision(26, 16);
        builder.Property(x => x.AmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.Property(x => x.DiscountRate).HasPrecision(26, 16);
        builder.Property(x => x.DiscountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.TotalAmountInBaseCurrency).HasPrecision(19, 4);

        builder.HasOne<Voucher>().WithMany()
            .HasForeignKey(x => x.VoucherRef).OnDelete(DeleteBehavior.NoAction);
    }
}
