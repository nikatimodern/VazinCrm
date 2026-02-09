using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Supplies.Configs
{
    public class SupplyOrderHeadConfigs : IEntityTypeConfiguration<SupplyOrderHead>
    {
        public void Configure(EntityTypeBuilder<SupplyOrderHead> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
