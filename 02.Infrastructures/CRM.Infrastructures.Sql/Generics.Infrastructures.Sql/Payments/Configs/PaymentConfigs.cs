using CRM.Core.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Payments.Configs
{
    public class PaymentConfigs : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasMany(f=>f.PaymentSupplyFactors)
                .WithOne()
                .HasForeignKey(f=>f.PaymentID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
