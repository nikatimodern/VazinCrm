using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class CashierUserConfigs : IEntityTypeConfiguration<CashierUser> {
    public void Configure(EntityTypeBuilder<CashierUser> builder)
    {
        builder.HasKey(k => new { k.CashierId, k.UserId });
    }
}