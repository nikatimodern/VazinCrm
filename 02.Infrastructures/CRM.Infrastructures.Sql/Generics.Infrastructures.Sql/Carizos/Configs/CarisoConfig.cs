using CRM.Core.Domain.Carizos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Carizos.Configs
{
    public class CarisoConfig : IEntityTypeConfiguration<Carizo>
    {
        public void Configure(EntityTypeBuilder<Carizo> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(f=>f.TaskUsers)
                .WithOne()
                .HasForeignKey(f=>f.TaskId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
