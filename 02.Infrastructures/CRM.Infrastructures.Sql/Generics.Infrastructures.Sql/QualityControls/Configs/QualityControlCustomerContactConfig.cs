using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlCustomerContactConfig : IEntityTypeConfiguration<QualityControlCustomerContact>
{
    public void Configure(EntityTypeBuilder<QualityControlCustomerContact> builder)
    {
        builder.ToTable("QualityControlCustomerContacts",
            table => table.HasCheckConstraint("CK_QualityControlCustomerContacts_Status", "[Status] IN (0, 1, 2)"));
        builder.HasKey(contact => contact.Id);
        builder.HasIndex(contact => contact.CustomerId).IsUnique();
        builder.Property(contact => contact.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(contact => contact.RowVersion).IsRowVersion();
        builder.HasOne<Customer>().WithMany().HasForeignKey(contact => contact.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(contact => !contact.IsDeleted);
    }
}
