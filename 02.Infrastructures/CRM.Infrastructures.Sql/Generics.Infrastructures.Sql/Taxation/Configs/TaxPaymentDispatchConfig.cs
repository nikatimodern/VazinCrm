using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Taxation.Configs;

internal sealed class TaxPaymentDispatchConfig : IEntityTypeConfiguration<TaxPaymentDispatch>
{
    public void Configure(EntityTypeBuilder<TaxPaymentDispatch> builder)
    {
        builder.HasOne<TaxDispatch>().WithMany().HasForeignKey(x => x.InvoiceDispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AccountancyId).IsUnique();
        builder.HasIndex(x => new { x.InvoiceDispatchId, x.ReferenceHash }).IsUnique();
        builder.Property(x => x.ReferenceHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Amount).HasPrecision(18, 0);
        builder.Property(x => x.ProtectedRequest).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(450);
        builder.ToTable("TaxPaymentDispatches", table =>
        {
            table.HasCheckConstraint("CK_TaxPaymentDispatches_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_TaxPaymentDispatches_State",
                "([Status] = 0 AND [AttemptId] IS NULL AND [AttemptStartedAt] IS NULL AND [ResolvedAt] IS NULL) OR " +
                "([Status] IN (1, 2) AND [AttemptId] IS NOT NULL AND [AttemptStartedAt] IS NOT NULL AND [ResolvedAt] IS NULL) OR " +
                "([Status] IN (3, 4) AND [AttemptId] IS NOT NULL AND [AttemptStartedAt] IS NOT NULL AND [ResolvedAt] IS NOT NULL)");
        });
    }
}
