#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Security;

/// <summary>Exact AccessCatalog keys and record ownership, evaluated from the current database.
/// External customer reads remain a separate, person-bound policy; they never grant employee operations.</summary>
public sealed class DocumentAccessPolicy(
    IHttpContextAccessor context, IConfiguration configuration, IAccessScopeService scopes,
    CRMContextDb db, UserContextDb users) : IDocumentReadAccess
{
    private string UserId => context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public bool IsEnforced(string entity) =>
        (entity is "PreFactor" or "Factor") &&
        configuration.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions");

    public async Task<bool> HasAsync(string entity, string action, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        return await scopes.GetEffectiveScopeAsync(UserId, entity + "." + action, ct) == AccessScopes.All;
    }

    private async Task<Guid?> ExternalPersonAsync(CancellationToken ct) =>
        await users.Users.AsNoTracking()
            .Where(u => u.Id == UserId && !u.IsEmployee && u.IsCustomer)
            .Select(u => u.PersonId).FirstOrDefaultAsync(ct);

    public async Task<IQueryable<PreFactor>> PreFactorsAsync(IQueryable<PreFactor> query, string action, CancellationToken ct)
    {
        if (!IsEnforced("PreFactor")) return query;
        if (action == "View" && await ExternalPersonAsync(ct) is Guid person)
            return query.Where(p => p.CustomerId == person);
        var owners = await scopes.GetDocumentOwnerAccessAsync(UserId, "PreFactor." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(p => false);
        bool supply = action is "ApproveTamin" or "EditSupply";
        bool view = action == "View";
        if (owners.AllowedOwnerIds is { } allowed)
        {
            var ids = allowed.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
            if (supply) query = query.Where(p => p.UserTaminId.HasValue && ids.Contains(p.UserTaminId.Value));
            else if (view) query = query.Where(p => allowed.Contains(p.UserCreatorID) ||
                                    (p.UserTaminId.HasValue && ids.Contains(p.UserTaminId.Value)));
            else query = query.Where(p => allowed.Contains(p.UserCreatorID));
        }
        if (owners.ExcludedOwnerIds.Length > 0)
        {
            var excluded = owners.ExcludedOwnerIds;
            var ids = excluded.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
            query = query.Where(p => !excluded.Contains(p.UserCreatorID) &&
                                     (!p.UserTaminId.HasValue || !ids.Contains(p.UserTaminId.Value)));
        }
        return query;
    }

    public async Task<IQueryable<Factor>> FactorsAsync(IQueryable<Factor> query, string action, CancellationToken ct)
    {
        if (!IsEnforced("Factor")) return query;
        if (action == "View" && await ExternalPersonAsync(ct) is Guid person)
            return query.Where(f => f.CustomerId == person);
        var owners = await scopes.GetDocumentOwnerAccessAsync(UserId, "Factor." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(f => false);
        if (owners.AllowedOwnerIds is { } allowed)
        {
            var ids = allowed.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
            if (action == "View")
                query = query.Where(f => allowed.Contains(f.SelerId) || allowed.Contains(f.UserCreatorID) ||
                                     (f.UserTaminId.HasValue && ids.Contains(f.UserTaminId.Value)));
            else query = query.Where(f => allowed.Contains(f.SelerId) || allowed.Contains(f.UserCreatorID));
        }
        if (owners.ExcludedOwnerIds.Length > 0)
        {
            var excluded = owners.ExcludedOwnerIds;
            var ids = excluded.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
            query = query.Where(f => !excluded.Contains(f.SelerId) && !excluded.Contains(f.UserCreatorID) &&
                                     (!f.UserTaminId.HasValue || !ids.Contains(f.UserTaminId.Value)));
        }
        return query;
    }

    public async Task<bool> CanAsync(string entity, string action, Guid id, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        if (id == Guid.Empty) return false;
        if (entity == "PreFactor")
        {
            var q = await PreFactorsAsync(db.PreFactors.AsNoTracking().Where(p => p.Id == id), "View", ct);
            if (action != "View") q = await PreFactorsAsync(q, action, ct);
            return await q.AnyAsync(ct);
        }
        if (entity == "Factor")
        {
            var q = await FactorsAsync(db.Factors.AsNoTracking().Where(f => f.Id == id), "View", ct);
            if (action != "View") q = await FactorsAsync(q, action, ct);
            return await q.AnyAsync(ct);
        }
        return false;
    }

    public async Task<bool> HasViewAsync(string entity, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        return await scopes.GetEffectiveScopeAsync(UserId, entity + ".View", ct) != AccessScopes.None ||
               await ExternalPersonAsync(ct) != null;
    }
}
