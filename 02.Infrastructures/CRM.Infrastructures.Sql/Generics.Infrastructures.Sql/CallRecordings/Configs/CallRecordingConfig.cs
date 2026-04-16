using CRM.Core.Domain.CallRecordings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.CallRecordings.Configs;

public class CallRecordingConfig : IEntityTypeConfiguration<CallRecording> {
    public void Configure(EntityTypeBuilder<CallRecording> builder)
    {
        builder.HasKey(x => x.Id);
    }
}