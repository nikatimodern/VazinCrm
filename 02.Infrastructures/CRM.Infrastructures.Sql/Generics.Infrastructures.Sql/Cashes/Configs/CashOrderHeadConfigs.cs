using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class CashOrderHeadConfigs : IEntityTypeConfiguration<CashOrderHead> {
    public void Configure(EntityTypeBuilder<CashOrderHead> builder)
    {
        builder.HasKey(f => new { f.CashId, f.OrderHeadId });
    }
}