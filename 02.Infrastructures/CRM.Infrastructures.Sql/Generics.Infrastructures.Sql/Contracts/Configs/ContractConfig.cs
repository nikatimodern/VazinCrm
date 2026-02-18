using CRM.Core.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Contracts.Configs {
    public class ContractConfig : IEntityTypeConfiguration<Contract> {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.fromDateSh).IsRequired();
            builder.Property(x => x.toDateSh).IsRequired();

        }
    }
}