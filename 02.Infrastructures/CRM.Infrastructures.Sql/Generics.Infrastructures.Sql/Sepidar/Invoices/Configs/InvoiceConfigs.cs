using CRM.Core.Domain.Sepidar.Invoices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Sepidar.Invoices.Configs
{
    public class InvoiceConfigs : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(f => f.InvoiceId);
            builder.HasMany(f=>f.InvoiceItems).WithOne().HasForeignKey(f=>f.InvoiceRef).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
