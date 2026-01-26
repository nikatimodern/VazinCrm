using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Sepidar.Quotations.Configs
{
    public class QuotationConfigs : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.HasKey(f => f.QuotationId);
        }
    }
}
