using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.WebPushSubscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.PushSubscriptions.Configs;

public class PushSubscriptionConfig : IEntityTypeConfiguration<WebPushSubscription> {
    public void Configure(EntityTypeBuilder<WebPushSubscription> builder)
    {

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => x.Endpoint)
            .IsUnique();

        builder.Property(x => x.P256dh)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Auth)
            .IsRequired()
            .HasMaxLength(100);
    }
}