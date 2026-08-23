using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Supplies.Configs
{
    public class SupplyFactorOrderHeadConfigs : IEntityTypeConfiguration<SupplyFactorOrderHead>
    {
        public void Configure(EntityTypeBuilder<SupplyFactorOrderHead> builder)
        {
            builder.HasKey(x =>new {x.SupplyFactorId,x.OrderHeadId } );
        }
    }
}
