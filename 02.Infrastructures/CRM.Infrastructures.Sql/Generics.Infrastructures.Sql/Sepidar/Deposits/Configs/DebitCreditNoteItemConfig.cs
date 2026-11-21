using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Deposits.Configs;

internal sealed class DebitCreditNoteItemConfig : IEntityTypeConfiguration<DebitCreditNoteItem>
{
    public void Configure(EntityTypeBuilder<DebitCreditNoteItem> builder)
    {
        builder.ToTable("DebitCreditNoteItem", "GNR");
        builder.HasKey(x => x.DebitCreditNoteItemID);
        builder.Property(x => x.DebitCreditNoteItemID).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.Property(x => x.AmountInBaseCurrency).HasPrecision(19, 4);
        builder.Property(x => x.Rate).HasPrecision(26, 16);
        builder.Property(x => x.Description).HasMaxLength(250);
        builder.Property(x => x.Description_En).HasMaxLength(250);

        builder.HasOne<DebitCreditNote>().WithMany()
            .HasForeignKey(x => x.DebitCreditNoteRef).OnDelete(DeleteBehavior.NoAction);
    }
}
