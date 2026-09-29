using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sms.Configs;

public class SmsConfig : IEntityTypeConfiguration<Core.Domain.Sms.Entities.ReceivedSms> {
    public void Configure(EntityTypeBuilder<Core.Domain.Sms.Entities.ReceivedSms> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.FarazId).IsUnique();
    }
}