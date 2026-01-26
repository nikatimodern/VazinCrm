using CRM.Core.Domain.PreFactors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.PreFactors.Configs
{
    public class PreFactorDescriptionConfig : IEntityTypeConfiguration<PreFactorDescription>
    {
        public void Configure(EntityTypeBuilder<PreFactorDescription> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}
