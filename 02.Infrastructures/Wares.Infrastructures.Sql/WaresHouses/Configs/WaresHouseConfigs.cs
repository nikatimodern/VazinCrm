using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wares.Core.Domain.WaresHouses.Entities;



namespace Wares.Infrastructures.Sql.Wares.Configs
{
    internal class WaresHouseConfigs : IEntityTypeConfiguration<WaresHouse>
    {
        public void Configure(EntityTypeBuilder<WaresHouse> builder)
        {
            builder.HasKey(t => t.Id);
        }
    }
}
