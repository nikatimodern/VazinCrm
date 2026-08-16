using CRM.Core.Domain.Accountancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Accountancies.Configs
{
    public class AccountancyConfig : IEntityTypeConfiguration<Accountancy>
    {
        public void Configure(EntityTypeBuilder<Accountancy> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(f => f.SerialNum).ValueGeneratedOnAdd();
            builder.Property(f => f.credit).HasPrecision(20, 0);
            builder.Property(f => f.debit).HasPrecision(20, 0);
            builder.Property(f => f.discount).HasPrecision(20, 0);
            builder.Property(f => f.tax).HasPrecision(20, 0);
            
            builder.HasOne(x => x.Deal)
                .WithMany(x => x.Accountancies)
                .HasForeignKey(x => x.DealId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
