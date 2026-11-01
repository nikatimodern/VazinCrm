#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Products.Repositories;
public sealed class ProductPriceRepository(CRMContextDb db) : IProductPriceRepository
{
    public Task<List<Product>> SearchAsync(string? search, CancellationToken ct) => db.Products.AsNoTracking().Where(p => EF.Functions.Like(p.Title, "%" + search + "%")).Include(p => p.ProductAmounts).ToListAsync(ct);
    public Task<ProductAmount?> GetAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.ProductAmounts : db.ProductAmounts.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task SaveAsync(ProductAmount price, bool create, CancellationToken ct)
    {
        if (create)
            db.ProductAmounts.Add(price);
        else
        {
            var current = await db.ProductAmounts.FirstAsync(p => p.Id == price.Id, ct);
            var entry = db.Entry(current);
            entry.CurrentValues.SetValues(price);
            foreach (var property in new[]
            {
                "CreateDate",
                "CreateDateTxt",
                "UserCreator",
                "UserCreatorID"
            }

            )
                entry.Property(property).IsModified = false;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
