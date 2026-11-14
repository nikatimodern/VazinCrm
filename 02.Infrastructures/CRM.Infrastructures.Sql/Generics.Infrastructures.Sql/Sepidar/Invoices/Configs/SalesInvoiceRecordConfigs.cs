using CRM.Core.Domain.Sepidar.Invoices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Sepidar.Invoices.Configs;

public sealed class SalesInvoiceRecordConfigs :
    IEntityTypeConfiguration<SalesInvoiceRecord>,
    IEntityTypeConfiguration<SalesInvoiceItemRecord>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceRecord> builder)
    {
        builder.HasKey(x => x.InvoiceId);
        builder.Property(x => x.InvoiceId).ValueGeneratedNever();

        builder.HasMany(x => x.InvoiceItems)
            .WithOne()
            .HasForeignKey(x => x.InvoiceRef)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<SalesInvoiceItemRecord> builder)
    {
        builder.HasKey(x => x.InvoiceItemID);
        builder.Property(x => x.InvoiceItemID).ValueGeneratedNever();
    }
}
