using CRM.Core.Domain.Reports.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace CRM.Infrastructures.Sql.Reports.Configs;

public class ReportParticipantConfig : IEntityTypeConfiguration<ReportParticipant> {
    public void Configure(EntityTypeBuilder<ReportParticipant> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.Report)
            .WithMany(f => f.Participants)
            .HasForeignKey(f => f.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}