using CRM.Core.Domain.Sepidar.Invoices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Sepidar.Invoices.Configs
{
    public class InvoiceItemConfigs : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.HasKey(f => f.InvoiceItemID);
         
        }
    }
}
