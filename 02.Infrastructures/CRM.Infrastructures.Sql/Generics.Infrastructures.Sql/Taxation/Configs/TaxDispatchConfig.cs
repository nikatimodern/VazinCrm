using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Taxation.Configs;

internal sealed class TaxDispatchConfig : IEntityTypeConfiguration<TaxDispatch>
{
    public void Configure(EntityTypeBuilder<TaxDispatch> builder)
    {
        builder.HasIndex(x => x.FactorId).IsUnique().HasFilter("[Kind] = 0");
        builder.HasIndex(x => new { x.FactorId, x.Kind }).IsUnique().HasFilter("[Kind] IN (1, 3)");
        builder.HasIndex(x => x.ReferencedDispatchId);
        builder.HasIndex(x => x.ReferencedDispatchId, "IX_TaxDispatches_OneCancellation").IsUnique()
            .HasFilter("[Kind] = 2");
        builder.HasOne<TaxDispatch>().WithMany().HasForeignKey(x => x.ReferencedDispatchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AuthorityEnvironment, x.FiscalId, x.InvoiceSerial }).IsUnique()
            .HasFilter("[InvoiceSerial] IS NOT NULL");
        builder.HasIndex(x => new { x.AuthorityEnvironment, x.TaxId }).IsUnique()
            .HasFilter("[TaxId] IS NOT NULL");
        builder.Property(x => x.CertificateSha256).HasMaxLength(64);
        builder.Property(x => x.AuthorityProtocol).HasDefaultValue(TaxAuthorityProtocol.V2);
        builder.Property(x => x.V1KeyFingerprint).HasMaxLength(64);
        builder.Property(x => x.TaxId).HasMaxLength(22);
        builder.Property(x => x.AuthorityProcessingStatus).HasMaxLength(16);
        builder.HasIndex(x => x.SnapshotId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.QueuedUtc });
        builder.HasOne<TaxInvoiceSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.FiscalId).IsRequired().HasMaxLength(6);
        builder.Property(x => x.MemoryTitle).IsRequired().HasMaxLength(120);
        builder.ToTable("TaxDispatches", table =>
        {
            table.HasCheckConstraint("CK_TaxDispatches_State",
                "([Status] = 0 AND [AttemptId] IS NULL AND [LeaseUntilUtc] IS NULL) OR " +
                "([Status] = 1 AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR " +
                "([Status] IN (2, 3, 4, 5) AND [AttemptId] IS NOT NULL AND [LeaseUntilUtc] IS NULL)");
            table.HasCheckConstraint("CK_TaxDispatches_Reference",
                "([Kind] = 0 AND [ReferencedDispatchId] IS NULL) OR " +
                "([Kind] IN (1, 2, 3) AND [ReferencedDispatchId] IS NOT NULL AND [ReferencedDispatchId] <> [Id])");
        });
    }
}
