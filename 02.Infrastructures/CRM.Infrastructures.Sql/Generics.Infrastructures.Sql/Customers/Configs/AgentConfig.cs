using CRM.Core.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Customers.Configs
{
    public class AgentConfig : IEntityTypeConfiguration<Agent>
    {
        public void Configure(EntityTypeBuilder<Agent> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(f => f.Contacts)
                .WithOne()
                .HasForeignKey(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
