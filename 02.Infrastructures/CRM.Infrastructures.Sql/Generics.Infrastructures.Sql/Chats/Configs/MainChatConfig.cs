using CRM.Core.Domain.Chats.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Chats.Configs;

public class MainChatConfig : IEntityTypeConfiguration<MainChat> {
    public void Configure(EntityTypeBuilder<MainChat> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasMany(f => f.Chats)
            .WithOne()
            .HasForeignKey(f => f.MainChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}