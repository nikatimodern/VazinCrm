using CRM.Core.Domain.SystemWarnings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.SystemWarnings.Configs;

public class SystemWarningConfigs: IEntityTypeConfiguration<SystemWarning>
{
    public void Configure(EntityTypeBuilder<SystemWarning> builder)
    {
        builder.HasKey(f => f.Id);
    }
}