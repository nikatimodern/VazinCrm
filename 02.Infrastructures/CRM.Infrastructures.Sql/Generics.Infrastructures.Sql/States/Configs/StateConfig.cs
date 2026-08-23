using CRM.Core.Domain.States.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.States.Configs
{
    public class StateConfig : IEntityTypeConfiguration<State>
    {
        public void Configure(EntityTypeBuilder<State> builder)
        {
            builder.HasKey(f => f.Id);
            builder.HasMany(f => f.Cities)
                   .WithOne(f => f.State)
                   .HasForeignKey(f => f.StateID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
