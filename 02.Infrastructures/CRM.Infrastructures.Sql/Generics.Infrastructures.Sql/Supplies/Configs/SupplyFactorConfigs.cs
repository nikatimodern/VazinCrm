using CRM.Core.Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Supplies.Configs
{
    public class SupplyFactorConfigs : IEntityTypeConfiguration<SupplyFactor>
    {
        public void Configure(EntityTypeBuilder<SupplyFactor> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasMany(f => f.SupplyFactorOrderHeads)
                .WithOne()
                .HasForeignKey(f => f.SupplyFactorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.RequestAmountSupplyFactor)
                .WithOne()
                .HasForeignKey(f => f.SupplyFactorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.PaymentSupplyFactors)
                .WithOne()
                .HasForeignKey(f => f.SupplyFactorID)
                .OnDelete(DeleteBehavior.Cascade);




        }
    }
}
