using CRM.Core.Domain.ASettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.ASettings.Configs
{
    public class ASettingsConfigs : IEntityTypeConfiguration<ASetting>
    {
        public void Configure(EntityTypeBuilder<ASetting> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}
