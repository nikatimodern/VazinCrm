using CRM.Core.Domain.CallRecordings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Reports.Configs;

public class ReportParticipantConfig : IEntityTypeConfiguration<CallParticipant> {
    public void Configure(EntityTypeBuilder<CallParticipant> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.CallRecording)
            .WithMany(f => f.Participants)
            .HasForeignKey(f => f.CallRecordingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Report)
            .WithMany(f => f.Participants)
            .HasForeignKey(f => f.ReportId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}