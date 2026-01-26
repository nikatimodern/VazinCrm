using CRM.Core.Domain.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Products.Configs
{
    public class ProductAmountConfig : IEntityTypeConfiguration<ProductAmount>
    {
        public void Configure(EntityTypeBuilder<ProductAmount> builder)
        {

            builder.Property(f => f.RasmiAmount).HasPrecision(20, 0);
            builder.Property(f => f.NonRasmiAmount).HasPrecision(20, 0);

        }
    }
}
