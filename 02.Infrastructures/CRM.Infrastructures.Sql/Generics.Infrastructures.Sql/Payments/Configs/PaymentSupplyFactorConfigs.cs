using CRM.Core.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Payments.Configs
{
    public class PaymentSupplyFactorConfigs : IEntityTypeConfiguration<PaymentSupplyFactor>
    {
        public void Configure(EntityTypeBuilder<PaymentSupplyFactor> builder)
        {
            builder.HasKey(f => new { PaymentID = f.PaymentId, SupplyFactorID = f.SupplyFactorId });
        }
    }
}
