using CRM.Core.Domain.RequestAmounts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.RequestAmounts.Configs;

public class RequestAmountOrderHeadConfigs : IEntityTypeConfiguration<RequestAmountOrderHead> {
    public void Configure(EntityTypeBuilder<RequestAmountOrderHead> builder)
    {
        builder.HasKey(x => new { x.OrderHeadId, x.RequestAmountId });
    }
}