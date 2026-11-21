using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class DebitCreditNoteConfig : IEntityTypeConfiguration<DebitCreditNote>
{
    public void Configure(EntityTypeBuilder<DebitCreditNote> builder)
    {
        builder.ToTable("DebitCreditNote", "GNR");
        builder.HasKey(x => x.DebitCreditNoteID);
        builder.Property(x => x.DebitCreditNoteID).ValueGeneratedNever();
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.CreationDate).HasColumnType("datetime");
        builder.Property(x => x.LastModificationDate).HasColumnType("datetime");
        builder.Property(x => x.Rate).HasPrecision(26, 16);
        builder.Property(x => x.SumAmount).HasPrecision(19, 4);
        builder.Property(x => x.SumAmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Description_En).HasMaxLength(4000);

        builder.HasOne<Voucher>().WithMany()
            .HasForeignKey(x => x.VoucherRef).OnDelete(DeleteBehavior.NoAction);
    }
}
