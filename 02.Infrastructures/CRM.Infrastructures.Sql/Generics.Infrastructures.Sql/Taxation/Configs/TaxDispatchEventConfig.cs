using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Taxation.Configs;

internal sealed class TaxDispatchEventConfig : IEntityTypeConfiguration<TaxDispatchEvent>
{
    public void Configure(EntityTypeBuilder<TaxDispatchEvent> builder)
    {
        builder.HasOne<TaxDispatch>().WithMany().HasForeignKey(x => x.DispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DispatchId, x.Id });
        builder.Property(x => x.Result).IsRequired().HasMaxLength(16);
    }
}
