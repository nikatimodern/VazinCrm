using CRM.Core.Domain.LogSystems.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using CRM.Core.Domain.Cartables.Entities;

namespace CRM.Infrastructures.Sql.LogSystems.Configs {
    public class LogSystemConfigs : IEntityTypeConfiguration<LogSystem> {
        public void Configure(EntityTypeBuilder<LogSystem> builder)
        {
            builder.HasKey(f => f.Id);
            builder.HasOne(f => f.Cartable).WithOne(f => f.LogSystem)
                .HasForeignKey<Cartable>(f => f.LogSystemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}