using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cashes.Configs;

public class BaseCashConfig : IEntityTypeConfiguration<BaseCash> {
    public void Configure(EntityTypeBuilder<BaseCash> builder)
    {
        builder.HasKey(f => f.Id);
    }
}