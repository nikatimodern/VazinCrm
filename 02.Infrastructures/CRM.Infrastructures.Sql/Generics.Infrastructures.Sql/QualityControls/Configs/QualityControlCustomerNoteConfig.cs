using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlCustomerNoteConfig : IEntityTypeConfiguration<QualityControlCustomerNote>
{
    public void Configure(EntityTypeBuilder<QualityControlCustomerNote> builder)
    {
        builder.ToTable("QualityControlCustomerNotes");
        builder.HasKey(note => note.Id);
        builder.HasIndex(note => new { note.CustomerId, note.CreateDate });
        // One customer-QC note per call or legacy review. Deleted rows still reserve the source.
        builder.HasIndex(note => note.CallRecordingId)
            .IsUnique().HasFilter("[CallRecordingId] IS NOT NULL");
        builder.HasIndex(note => note.LegacyQualityControlId)
            .IsUnique().HasFilter("[LegacyQualityControlId] IS NOT NULL");
        builder.Property(note => note.Description).IsRequired().HasMaxLength(4000);
        builder.Property(note => note.Source).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(note => note.RowVersion).IsRowVersion();
        builder.HasOne<Customer>().WithMany().HasForeignKey(note => note.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(note => !note.IsDeleted);
    }
}
