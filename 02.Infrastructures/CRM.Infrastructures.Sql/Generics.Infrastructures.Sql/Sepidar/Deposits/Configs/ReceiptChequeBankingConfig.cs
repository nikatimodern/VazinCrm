using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Banks.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptChequeBankingConfig : IEntityTypeConfiguration<ReceiptChequeBanking>
{
    public void Configure(EntityTypeBuilder<ReceiptChequeBanking> builder)
    {
        builder.ToTable("ReceiptChequeBanking", "RPA");
        builder.HasKey(x => x.ReceiptChequeBankingId);
        builder.Property(x => x.ReceiptChequeBankingId).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.CreationDate).HasColumnType("datetime");
        builder.Property(x => x.LastModificationDate).HasColumnType("datetime");
        builder.Property(x => x.SubmitNumber).HasMaxLength(20);
        builder.HasOne<SepidarBankAccount>().WithMany().HasForeignKey(x => x.BankAccountRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Voucher>().WithMany().HasForeignKey(x => x.VoucherRef).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ReceiptChequeBanking>().WithMany().HasForeignKey(x => x.ReceiptChequeBankingRef).OnDelete(DeleteBehavior.NoAction);
    }
}
