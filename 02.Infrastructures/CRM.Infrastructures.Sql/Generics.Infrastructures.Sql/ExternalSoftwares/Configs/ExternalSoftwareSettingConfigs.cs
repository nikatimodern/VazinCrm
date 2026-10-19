using CRM.Core.Domain.ExternalSoftwares.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.ExternalSoftwares.Configs;

public class ExternalSoftwareSettingConfigs : IEntityTypeConfiguration<ExternalSoftwareSetting> {
    public void Configure(EntityTypeBuilder<ExternalSoftwareSetting> builder)
    {
        builder.HasKey(f => f.Id);
    }
}