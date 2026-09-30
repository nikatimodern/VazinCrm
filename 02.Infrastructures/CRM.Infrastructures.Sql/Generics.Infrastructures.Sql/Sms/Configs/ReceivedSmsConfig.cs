using CRM.Core.Domain.Sms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sms.Configs;

public class ReceivedSmsConfig : IEntityTypeConfiguration<ReceivedSms> {
    public void Configure(EntityTypeBuilder<ReceivedSms> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.FarazId).IsUnique();
    }
}