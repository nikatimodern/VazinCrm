using CRM.Core.Domain.Sepidar.ExtraDataes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.ExtraDataes.Configs
{
    public class ExtraDataConfigs : IEntityTypeConfiguration<ExtraData>
    {
        public void Configure(EntityTypeBuilder<ExtraData> builder)
        {
           
        }
    }
}
