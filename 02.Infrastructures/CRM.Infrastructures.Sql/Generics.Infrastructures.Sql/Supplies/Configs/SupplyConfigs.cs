using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Supplies.Configs
{
    public class SupplyConfigs : IEntityTypeConfiguration<Supply>
    {
        public void Configure(EntityTypeBuilder<Supply> builder)
        {
            builder.Property(f => f.UnitAmount).HasPrecision(20, 0);
            builder.Property(f => f.TotalAmount).HasPrecision(20, 0);
            builder.HasKey(x => x.Id);
        }
    }
}
