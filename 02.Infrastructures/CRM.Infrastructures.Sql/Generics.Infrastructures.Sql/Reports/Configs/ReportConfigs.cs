using CRM.Core.Domain.Reports.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Infrastructures.Sql.Reports.Configs {
    public class ReportConfigs : IEntityTypeConfiguration<Report> {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasOne(f => f.CallRecording)
                .WithOne(c => c.Report)
                .HasForeignKey<Report>(f => f.CallRecordingId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}