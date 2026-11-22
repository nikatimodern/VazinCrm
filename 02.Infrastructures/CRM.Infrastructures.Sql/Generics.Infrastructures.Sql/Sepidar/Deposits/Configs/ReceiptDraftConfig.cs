using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class ReceiptDraftConfig : IEntityTypeConfiguration<ReceiptDraft>
{
    public void Configure(EntityTypeBuilder<ReceiptDraft> builder)
    {
        builder.ToTable("ReceiptDraft", "RPA");
        builder.HasKey(x => x.ReceiptDraftId);
        builder.Property(x => x.ReceiptDraftId).ValueGeneratedNever();
        builder.Property(x => x.HeaderDate).HasColumnType("datetime");
        builder.Property(x => x.Date).HasColumnType("datetime");
        builder.Property(x => x.Number).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Description_En).HasMaxLength(4000);
        builder.Property(x => x.Rate).HasPrecision(26, 16);
        builder.Property(x => x.AmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Amount).HasPrecision(19, 4);

        builder.HasOne<ReceiptHeader>().WithMany()
            .HasForeignKey(x => x.ReceiptHeaderRef).OnDelete(DeleteBehavior.NoAction);
    }
}
