using CRM.Core.Domain.Cashes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Cashes.Configs
{
    public class CashierConfigs : IEntityTypeConfiguration<Cashier>
    {
        public void Configure(EntityTypeBuilder<Cashier> builder)
        {
            builder.HasKey(f => f.Id);
            builder.HasMany(f=>f.Cashes)
                .WithOne()
                .HasForeignKey(f => f.CashierId)
                .OnDelete(DeleteBehavior.Cascade);
                


            
        }
    }

}
