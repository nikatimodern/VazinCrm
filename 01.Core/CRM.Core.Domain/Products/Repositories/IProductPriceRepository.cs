#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Products.Entities;

namespace CRM.Core.Domain.Products.Repositories;
public interface IProductPriceRepository
{
    Task<List<Product>> SearchAsync(string? search, CancellationToken cancellationToken);
    Task<ProductAmount?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task SaveAsync(ProductAmount price, bool create, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
