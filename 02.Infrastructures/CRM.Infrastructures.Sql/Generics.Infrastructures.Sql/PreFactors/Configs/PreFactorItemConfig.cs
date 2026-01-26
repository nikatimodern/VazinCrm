using CRM.Core.Domain.PreFactors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.PreFactors.Configs
{
    public class PreFactorItemConfig : IEntityTypeConfiguration<PreFactorItem>
    {
        public void Configure(EntityTypeBuilder<PreFactorItem> builder)
        {

            builder.HasKey(f => f.Id);
            builder.Property(f => f.UnitAmount).HasPrecision(20, 0);
            builder.Property(f => f.TotalAmount).HasPrecision(20, 0);
            builder.Property(f => f.TaminUnitAmount).HasPrecision(20, 0);
            builder.Property(f => f.TaminTotalAmount).HasPrecision(20, 0);
            builder.HasMany(f => f.supplies)
                .WithOne()
                .HasForeignKey(f => f.PfItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
