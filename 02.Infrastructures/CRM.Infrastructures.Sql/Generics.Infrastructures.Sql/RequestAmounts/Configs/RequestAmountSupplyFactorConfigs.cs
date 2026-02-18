using CRM.Core.Domain.RequestAmounts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.RequestAmounts.Configs
{
    public class RequestAmountSupplyFactorConfigs : IEntityTypeConfiguration<RequestAmountSupplyFactor>
    {
        public void Configure(EntityTypeBuilder<RequestAmountSupplyFactor> builder)
        {
            builder.HasKey(x => new {x.SupplyFactorId,x.RequestAmountId } );



        }
    }
}
