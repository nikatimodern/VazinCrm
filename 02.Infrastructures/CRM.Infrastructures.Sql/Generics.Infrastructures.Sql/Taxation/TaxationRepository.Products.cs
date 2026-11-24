using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed partial class TaxationRepository
{
    public async Task<TaxProductPage> ListProductsAsync(string search, bool unmapped, int page, int size, CancellationToken ct)
    {
        var mappings = db.TaxProductMappings.AsNoTracking().Where(x => x.IsDefault);
        var codes = TaxUnitCatalog.Codes.ToArray();
        var validMappings = mappings.Where(x => codes.Contains(EF.Functions.Collate(x.TaxUnit, "Latin1_General_100_BIN2")) &&
            EF.Functions.DataLength(x.TaxUnit) == x.TaxUnit.Length * 2);
        var pricedUnits = db.ProductAmounts.AsNoTracking().Where(x => !x.IsDeleted && x.ProductId != null && x.Unit != null)
            .Select(x => new { ProductId = x.ProductId.Value, Unit = x.Unit.Value, Title = x.UnitTxt });
        var invoicedUnits = db.FactorItems.AsNoTracking().Where(x => !x.IsDeleted && x.ProductCode != null)
            .Select(x => new { ProductId = x.ProductCode.Value, x.Unit, Title = x.UnitTxt });
        var units = pricedUnits.Concat(invoicedUnits);
        var products = db.Products.AsNoTracking().Where(x => !x.IsDeleted);
        if (search.Length > 0)
            products = products.Where(x => x.Title.Contains(search) ||
                mappings.Any(mapping => mapping.ProductId == x.Id && mapping.GoodsId.Contains(search)));
        if (unmapped)
            products = products.Where(product =>
                !validMappings.Any(mapping => mapping.ProductId == product.Id) ||
                units.Any(unit => unit.ProductId == product.Id && !validMappings.Any(mapping =>
                    mapping.ProductId == product.Id && mapping.Unit == unit.Unit)) ||
                mappings.Any(mapping => mapping.ProductId == product.Id && !validMappings.Any(valid => valid.Id == mapping.Id)));

        var count = await products.CountAsync(ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(count / (decimal)size));
        page = Math.Min(page, pageCount);
        var selected = await products.OrderBy(x => x.Title).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new { x.Id, x.Title }).ToListAsync(ct);
        var ids = selected.Select(x => x.Id).ToArray();
        if (ids.Length == 0)
            return new([], page, pageCount);

        var unitRows = await units.Where(x => ids.Contains(x.ProductId)).GroupBy(x => new { x.ProductId, x.Unit })
            .Select(group => new { group.Key.ProductId, group.Key.Unit, Title = group.Max(x => x.Title) }).ToListAsync(ct);
        var mappingRows = await mappings.Where(x => ids.Contains(x.ProductId)).Select(x => new
        {
            x.ProductId, x.Unit, x.GoodsId, x.TaxUnit
        }).ToListAsync(ct);
        var unitTitles = unitRows.ToDictionary(x => (x.ProductId, x.Unit), x => x.Title);
        var byUnit = mappingRows.ToDictionary(x => (x.ProductId, x.Unit));
        var allUnits = unitTitles.Keys.Union(byUnit.Keys).ToLookup(x => x.ProductId);
        return new(selected.Select(product => new TaxProductRow(product.Id, product.Title,
            allUnits[product.Id].OrderBy(x => x.Unit).Select(key =>
            {
                unitTitles.TryGetValue(key, out var title);
                byUnit.TryGetValue(key, out var mapping);
                return new TaxProductUnit(key.Unit, title, mapping?.GoodsId, mapping?.TaxUnit);
            }).ToArray())).ToArray(), page, pageCount);
    }
}
