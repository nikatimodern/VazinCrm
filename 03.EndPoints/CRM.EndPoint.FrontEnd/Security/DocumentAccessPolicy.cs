#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Framework.Domain;
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
    private readonly Dictionary<string, Task<DealOwnerAccess>> ownerCache = new(StringComparer.Ordinal);
    private Task<Guid?>? externalPerson;
    private Task<DealOwnerAccess> OwnersAsync(string permission, CancellationToken ct)
    {
        if (!ownerCache.TryGetValue(permission, out var cached))
            ownerCache[permission] = cached = scopes.GetDocumentOwnerAccessAsync(UserId, permission, ct);
        return cached;
    }

    private string UserId => context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public bool IsEnforced(string entity) =>
        (entity is "PreFactor" or "Factor" or "OrderHead" or "Supply" or "SupplyFactor" or "Accountancy" or "RequestAmount" or "Payment" or "BankAccount" or "Checkbook" or "Check") &&
        configuration.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions");

    public async Task<bool> HasAsync(string entity, string action, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        return await scopes.GetEffectiveScopeAsync(UserId, entity + "." + action, ct) == AccessScopes.All;
    }

    public async Task<bool> HasAnyAsync(string entity, string action, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        var owners = await OwnersAsync(entity + "." + action, ct);
        return owners.AllowedOwnerIds is not { Length: 0 };
    }

    private Task<Guid?> ExternalPersonAsync(CancellationToken ct) => externalPerson ??=
        users.Users.AsNoTracking()
            .Where(u => u.Id == UserId && !u.IsEmployee && u.IsCustomer)
            .Select(u => u.PersonId).FirstOrDefaultAsync(ct);

    public async Task<IQueryable<PreFactor>> PreFactorsAsync(IQueryable<PreFactor> query, string action, CancellationToken ct)
    {
        if (!IsEnforced("PreFactor")) return query;
        if (action == "View" && await ExternalPersonAsync(ct) is Guid person)
            return query.Where(p => p.CustomerId == person);
        var owners = await OwnersAsync("PreFactor." + action, ct);
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
        var owners = await OwnersAsync("Factor." + action, ct);
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

    // Row-level checks for Razor cards. Scope resolution is cached for this
    // request; no additional per-card record queries are needed.
    private static bool AllowsOwners(DealOwnerAccess access, string?[] included, string?[] excluded)
    {
        if (access.AllowedOwnerIds is { Length: 0 }) return false;
        if (excluded.Any(id => id != null && access.ExcludedOwnerIds.Contains(id, StringComparer.OrdinalIgnoreCase))) return false;
        return access.AllowedOwnerIds == null ||
               included.Any(id => id != null && access.AllowedOwnerIds.Contains(id, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> AllowsPreFactorAsync(PreFactor row, string action, CancellationToken ct)
    {
        if (!IsEnforced("PreFactor")) return true;
        if (action == "View" && await ExternalPersonAsync(ct) is Guid person) return row.CustomerId == person;
        if (action != "View" && !await AllowsPreFactorAsync(row, "View", ct)) return false;
        var all = new string?[] { row.UserCreatorID, row.UserTaminId?.ToString() };
        var include = action is "ApproveTamin" or "EditSupply" ? new string?[] { row.UserTaminId?.ToString() }
            : action == "View" ? all : new string?[] { row.UserCreatorID };
        return AllowsOwners(await OwnersAsync("PreFactor." + action, ct), include, all);
    }

    public async Task<bool> AllowsFactorAsync(Factor row, string action, CancellationToken ct)
    {
        if (!IsEnforced("Factor")) return true;
        if (action == "View" && await ExternalPersonAsync(ct) is Guid person) return row.CustomerId == person;
        if (action != "View" && !await AllowsFactorAsync(row, "View", ct)) return false;
        var all = new string?[] { row.SelerId, row.UserCreatorID, row.UserTaminId?.ToString() };
        var include = action == "View" ? all : new string?[] { row.SelerId, row.UserCreatorID };
        return AllowsOwners(await OwnersAsync("Factor." + action, ct), include, all);
    }

    public async Task<IQueryable<Accountancy>> ReceivedChecksAsync(IQueryable<Accountancy> query, CancellationToken ct)
    {
        if (!IsEnforced("Check")) return query;
        var owners = await OwnersAsync("Check.View", ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        if (owners.AllowedOwnerIds is { } allowed) query = query.Where(x => allowed.Contains(x.UserCreatorID));
        if (owners.ExcludedOwnerIds.Length > 0)
        {
            var excluded = owners.ExcludedOwnerIds;
            query = query.Where(x => !excluded.Contains(x.UserCreatorID));
        }
        return query;
    }

    public async Task<IQueryable<T>> ApplyAsync<T>(IQueryable<T> query, string action, CancellationToken ct) where T : BaseEntity
    {
        var entity = typeof(T).Name;
        if (!IsEnforced(entity)) return query;
        var owners = await OwnersAsync(entity + "." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        var allowed = owners.AllowedOwnerIds;
        var excluded = owners.ExcludedOwnerIds;
        if (typeof(T) == typeof(OrderHead))
        {
            var heads = (IQueryable<OrderHead>)(object)query;
            if (allowed != null)
            {
                var ids = allowed.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                heads = heads.Where(x => allowed.Contains(x.UserCreatorID) || (x.UserTaminId.HasValue && ids.Contains(x.UserTaminId.Value)));
            }
            if (excluded.Length > 0)
            {
                var ids = excluded.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                heads = heads.Where(x => !excluded.Contains(x.UserCreatorID) && (!x.UserTaminId.HasValue || !ids.Contains(x.UserTaminId.Value)));
            }
            return (IQueryable<T>)(object)heads;
        }
        if (typeof(T) == typeof(Supply))
        {
            var supplies = (IQueryable<Supply>)(object)query;
            if (allowed != null)
            {
                var ids = allowed.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                supplies = supplies.Where(x => allowed.Contains(x.UserCreatorID) || db.PreFactors.Any(p => p.Id == x.PfId && p.UserTaminId.HasValue && ids.Contains(p.UserTaminId.Value)));
            }
            if (excluded.Length > 0)
            {
                var ids = excluded.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                supplies = supplies.Where(x => !excluded.Contains(x.UserCreatorID) && !db.PreFactors.Any(p => p.Id == x.PfId && p.UserTaminId.HasValue && ids.Contains(p.UserTaminId.Value)));
            }
            return (IQueryable<T>)(object)supplies;
        }
        if (allowed != null) query = query.Where(x => allowed.Contains(x.UserCreatorID));
        if (excluded.Length > 0) query = query.Where(x => !excluded.Contains(x.UserCreatorID));
        return query;
    }

    public async Task<bool> AllowsRowAsync<T>(T row, string action, CancellationToken ct) where T : BaseEntity
    {
        var entity = typeof(T).Name;
        if (!IsEnforced(entity)) return true;
        if (action != "View" && !await AllowsRowAsync(row, "View", ct)) return false;
        if (row is OrderHead head)
        {
            var owners = new string?[] { head.UserCreatorID, head.UserTaminId?.ToString() };
            return AllowsOwners(await OwnersAsync(entity + "." + action, ct), owners, owners);
        }
        if (row is Supply) return await CanAsync(entity, action, row.Id, ct);
        var creator = new string?[] { row.UserCreatorID };
        return AllowsOwners(await OwnersAsync(entity + "." + action, ct), creator, creator);
    }

    private async Task<bool> CanRowAsync<T>(IQueryable<T> query, string action, CancellationToken ct) where T : BaseEntity
    {
        query = await ApplyAsync(query, "View", ct);
        if (action != "View") query = await ApplyAsync(query, action, ct);
        return await query.AnyAsync(ct);
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
        return entity switch
        {
            "OrderHead" => await CanRowAsync(db.OrderHeads.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Supply" => await CanRowAsync(db.Supplies.AsNoTracking().Where(x => x.Id == id), action, ct),
            "SupplyFactor" => await CanRowAsync(db.SupplyFactors.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Accountancy" => await CanRowAsync(db.Accountancies.AsNoTracking().Where(x => x.Id == id), action, ct),
            "RequestAmount" => await CanRowAsync(db.RequestAmounts.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Payment" => await CanRowAsync(db.Payments.AsNoTracking().Where(x => x.Id == id), action, ct),
            "BankAccount" => await CanRowAsync(db.BankAccounts.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Checkbook" => await CanRowAsync(db.Checkbooks.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Check" => await CanRowAsync(db.Checks.AsNoTracking().Where(x => x.Id == id), action, ct),
            _ => false
        };
    }

    public async Task<bool> HasViewAsync(string entity, CancellationToken ct)
    {
        if (!IsEnforced(entity)) return true;
        return await scopes.GetEffectiveScopeAsync(UserId, entity + ".View", ct) != AccessScopes.None ||
               ((entity is "PreFactor" or "Factor") && await ExternalPersonAsync(ct) != null);
    }
}
