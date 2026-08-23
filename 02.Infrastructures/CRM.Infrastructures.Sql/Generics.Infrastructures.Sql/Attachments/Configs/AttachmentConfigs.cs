using CRM.Core.Domain.Attachments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Attachments.Configs
{
    public class AttachmentConfigs : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasIndex(x => x.EntityId);
        }
    }
}
