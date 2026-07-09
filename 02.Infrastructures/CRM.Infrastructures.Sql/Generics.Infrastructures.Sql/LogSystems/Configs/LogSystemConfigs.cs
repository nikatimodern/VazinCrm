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

            builder.Property(x => x.EntityName).HasMaxLength(100);
            builder.Property(x => x.ParentEntityName).HasMaxLength(100);
            builder.Property(x => x.EntityId).HasMaxLength(100);
            builder.Property(x => x.ParentEntityId).HasMaxLength(100);
            builder.HasIndex(x => new { x.EntityName, x.EntityId });
            builder.HasIndex(x => new { x.ParentEntityName, x.ParentEntityId });
        }
    }
}