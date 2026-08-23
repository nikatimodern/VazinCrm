using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs {
    public class CashierConfigs : IEntityTypeConfiguration<Cashier> {
        public void Configure(EntityTypeBuilder<Cashier> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}