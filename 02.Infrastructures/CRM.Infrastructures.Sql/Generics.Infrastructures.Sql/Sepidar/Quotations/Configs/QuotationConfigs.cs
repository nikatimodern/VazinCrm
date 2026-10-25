using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Quotations.Configs
{
    public class QuotationConfigs : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.HasMany(f => f.QuotationItems)
                .WithOne()
                .HasForeignKey(f => f.QuotationRef)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
