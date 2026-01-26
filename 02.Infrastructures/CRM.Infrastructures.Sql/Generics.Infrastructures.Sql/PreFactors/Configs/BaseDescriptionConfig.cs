using CRM.Core.Domain.PreFactors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.PreFactors.Configs
{
    public class BaseDescriptionConfig : IEntityTypeConfiguration<BaseDescription>
    {
        public void Configure(EntityTypeBuilder<BaseDescription> builder)
        {
            builder.HasKey(f => f.Id);
        }
    }
}
