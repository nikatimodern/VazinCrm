using System;
using System.Collections.Generic;
using System.Linq;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed class AccessModelCatalog(CRMContextDb db) : IAccessModelCatalog
{
    public IReadOnlyList<AccessModelOption> GetCandidates()
    {
        var exposed = typeof(CRMContextDb).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        return db.Model.GetEntityTypes()
            .Where(entity => entity.ClrType is not null && exposed.Contains(entity.ClrType) &&
                             !entity.IsOwned() && entity.FindPrimaryKey() is not null &&
                             entity.ClrType.Namespace?.StartsWith("CRM.Core.Domain.", StringComparison.Ordinal) == true)
            .Select(entity => new AccessModelOption(nameof(CRMContextDb), entity.ClrType.FullName!, entity.ClrType.Name))
            .Distinct()
            .OrderBy(x => x.ModelName)
            .ToArray();
    }
}
