using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Taxation.Configs;

internal static class TaxationModel
{
    internal static void Configure(ModelBuilder model)
    {
        var memory = model.Entity<TaxMemory>();
        memory.Property(x => x.FiscalId).IsRequired().HasMaxLength(6);
        memory.Property(x => x.Title).IsRequired().HasMaxLength(120);
        memory.HasIndex(x => x.FiscalId).IsUnique();
        memory.HasIndex(x => x.IsActive).IsUnique().HasFilter("[IsActive] = 1");

        var mapping = model.Entity<TaxProductMapping>();
        mapping.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        mapping.Property(x => x.GoodsId).IsRequired().HasMaxLength(13);
        mapping.Property(x => x.TaxUnit).IsRequired().HasMaxLength(16);
        mapping.HasIndex(x => new { x.ProductId, x.Unit, x.GoodsId }).IsUnique();
        mapping.HasIndex(x => new { x.ProductId, x.Unit }).IsUnique().HasFilter("[IsDefault] = 1");

        var snapshot = model.Entity<TaxInvoiceSnapshot>();
        snapshot.HasOne<TaxMemory>().WithMany().HasForeignKey(x => x.TaxMemoryId).OnDelete(DeleteBehavior.Restrict);
        snapshot.HasOne<Factor>().WithMany().HasForeignKey(x => x.FactorId).OnDelete(DeleteBehavior.Restrict);
        snapshot.Property(x => x.CreatedBy).IsRequired().HasMaxLength(450);
        snapshot.Property(x => x.ProtectedPreview).IsRequired();
        snapshot.HasIndex(x => new { x.FactorId, x.CreatedUtc });
    }
}
