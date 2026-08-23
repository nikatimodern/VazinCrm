using CRM.Core.Domain.Cities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Cities.Configs
{
    public class CityConfig : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.HasKey(f => f.Id);
            //builder.HasMany(f => f.persons).WithOne(f => f.city).HasForeignKey(f => f.CityID)
            //    .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
