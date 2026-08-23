using CRM.Core.Domain.Products.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Products.Configs
{
    public class ProductGroupConfig : IEntityTypeConfiguration<ProductGroup>
    {
        public void Configure(EntityTypeBuilder<ProductGroup> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(f => f.Product)
                .WithOne().HasForeignKey(f => f.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
