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
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Contacts.Entities;
using CRM.Core.Domain.Contacts.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.CallRecordings.Entities;
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
        (entity is "PreFactor" or "Factor" or "OrderHead" or "Supply" or "SupplyFactor" or "Accountancy" or "RequestAmount" or "Payment" or "BankAccount" or "Checkbook" or "Check" or "Transfer" or "Cashier" or "Cash" or "BaseCash" or "BaseCenterCash" or "CashGroup" or "Warehouse" or "WarehouseProduct" or "WarehouseTransaction" or "WarehouseVoucher" or "QualityControl" or "QualityControlQuestion" or "Product" or "ProductGroup" or "ProductAmount" or "Contract" or "Carizo" or "Report" or "CallRecording" or "Chat" or "Email" or "Contact" or "Agent" or "ReleaseNote" or "LogSystem" or "SystemWarning" or "Company" or "ASetting" or "BaseItem" or "BaseDescription" or "ExternalSoftware" or "ExternalMapping" or "SentSms" or "ReceivedSms" or "Dashboard") &&
        configuration.GetValue<bool>("AccessCatalog:Enforce" + entity + "Permissions");

    public async Task<IQueryable<Customer>> CustomersAsync(IQueryable<Customer> query, string action, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions")) return query;
        var filter = await scopes.GetCustomerOwnerAccessAsync(UserId, "Customer." + action, ct);
        if (filter.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        if (filter.AllowedOwnerIds is { } allowed)
            query = query.Where(x => db.UserCustomers.Any(u => u.CustomerId == x.Id && allowed.Contains(u.UserId)));
        if (filter.ExcludedOwnerIds.Length > 0)
        {
            var excluded = filter.ExcludedOwnerIds;
            query = query.Where(x => !db.UserCustomers.Any(u => u.CustomerId == x.Id && excluded.Contains(u.UserId)));
        }
        return query;
    }

    public async Task<IQueryable<Deal>> DealsAsync(IQueryable<Deal> query, string action, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("AccessCatalog:EnforceDealPermissions")) return query;
        var filter = await scopes.GetDealOwnerAccessAsync(UserId, "Deal." + action, ct);
        if (filter.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        if (filter.AllowedOwnerIds is { } allowed) query = query.Where(x => allowed.Contains(x.UserCreatorID));
        if (filter.ExcludedOwnerIds.Length > 0)
        {
            var excluded = filter.ExcludedOwnerIds;
            query = query.Where(x => !excluded.Contains(x.UserCreatorID));
        }
        return query;
    }

    public async Task<bool> CanCustomerAsync(Guid id, string action, CancellationToken ct)
    {
        var query = await CustomersAsync(db.Customers.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id), "View", ct);
        if (action != "View") query = await CustomersAsync(query, action, ct);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> CanDealAsync(Guid id, string action, CancellationToken ct)
    {
        var query = await DealsAsync(db.Deals.AsNoTracking().Where(x => x.Id == id), "View", ct);
        if (action != "View") query = await DealsAsync(query, action, ct);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> CanChatAsync(Guid id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(UserId) || !await HasAsync("Chat", "View", ct)) return false;
        return await db.Chats.AsNoTracking().AnyAsync(m => m.Id == id &&
            db.MainChats.Any(c => c.Id == m.MainChatId && (c.FromUserId == UserId || c.ToUserId == UserId)), ct);
    }

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

    public async Task<bool> HasDashboardSourceViewAsync(string entity, CancellationToken ct)
    {
        if (entity == "Customer")
            return !configuration.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions") ||
                await scopes.GetEffectiveScopeAsync(UserId, "Customer.View", ct) != AccessScopes.None;
        return await HasViewAsync(entity, ct);
    }

    public async Task<IQueryable<T>> ApplyDashboardOwnersAsync<T>(IQueryable<T> query, string action, string ownerProperty, CancellationToken ct)
        where T : class
    {
        if (!IsEnforced("Dashboard")) return query;
        if (!CRM.Core.Domain.AccessCatalog.Modules.Dashboard.SupportsScope(action)) return query.Where(x => false);
        var owners = await OwnersAsync("Dashboard." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        if (owners.AllowedOwnerIds is { } allowed)
            query = query.Where(x => allowed.Contains(EF.Property<string>(x, ownerProperty)));
        if (owners.ExcludedOwnerIds.Length > 0)
        {
            var excluded = owners.ExcludedOwnerIds;
            query = query.Where(x => !excluded.Contains(EF.Property<string>(x, ownerProperty)));
        }
        return query;
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

    private static bool IsWarehouseEntity(string entity) => entity is "Warehouse" or "WarehouseProduct" or "WarehouseTransaction" or "WarehouseVoucher";
    private bool WarehouseEnforced => IsEnforced("Warehouse") || IsEnforced("WarehouseProduct") || IsEnforced("WarehouseTransaction") || IsEnforced("WarehouseVoucher");

    public async Task<bool> CanWarehouseScopeAsync(string entity, string action, Guid warehouseId, CancellationToken ct)
    {
        if (!WarehouseEnforced) return true;
        if (!IsWarehouseEntity(entity) || warehouseId == Guid.Empty) return false;
        var visible = await WarehouseOwnersAsync("Warehouse", "View", ct);
        var entityView = await WarehouseOwnersAsync(entity, "View", ct);
        var actionScope = await WarehouseOwnersAsync(entity, action, ct);
        return await visible.AnyAsync(w => w.Id == warehouseId && entityView.Any(v => v.Id == w.Id) && actionScope.Any(v => v.Id == w.Id), ct);
    }

    // Warehouse scope is shared by its ledger, products and vouchers. A warehouse's
    // creator and assigned employees are its owners; an exclusion wins over inclusion.
    private async Task<IQueryable<Warehouse>> WarehouseOwnersAsync(string entity, string action, CancellationToken ct)
    {
        var query = db.Warehouses.AsNoTracking();
        if (!IsEnforced(entity)) return query;
        var owners = await OwnersAsync(entity + "." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        if (owners.AllowedOwnerIds is { } allowed)
            query = query.Where(x => allowed.Contains(x.UserCreatorID) || x.WarehouseUsers.Any(u => allowed.Contains(u.UserId)));
        if (owners.ExcludedOwnerIds.Length > 0)
        {
            var excluded = owners.ExcludedOwnerIds;
            query = query.Where(x => !excluded.Contains(x.UserCreatorID) && !x.WarehouseUsers.Any(u => excluded.Contains(u.UserId)));
        }
        return query;
    }

    private async Task<IQueryable<T>> WarehouseScopeAsync<T>(IQueryable<T> query, string entity, string action, CancellationToken ct) where T : BaseEntity
    {
        var warehouses = await WarehouseOwnersAsync("Warehouse", "View", ct);
        var actionWarehouses = await WarehouseOwnersAsync(entity, action, ct);
        warehouses = warehouses.Where(w => actionWarehouses.Any(a => a.Id == w.Id));
        if (entity == "Warehouse")
        {
            var rows = (IQueryable<Warehouse>)(object)query;
            return (IQueryable<T>)(object)rows.Where(w => warehouses.Any(a => a.Id == w.Id));
        }
        if (entity == "WarehouseProduct")
        {
            var rows = (IQueryable<WarehouseProduct>)(object)query;
            return (IQueryable<T>)(object)rows.Where(p => warehouses.Any(w => w.Id == p.WarehouseId));
        }
        var productWarehouses = await WarehouseOwnersAsync("WarehouseProduct", "View", ct);
        var products = db.WarehouseProducts.AsNoTracking().Where(p => warehouses.Any(w => w.Id == p.WarehouseId) && productWarehouses.Any(w => w.Id == p.WarehouseId));
        if (entity == "WarehouseTransaction")
        {
            var rows = (IQueryable<WarehouseTransaction>)(object)query;
            var voucherWarehouses = await WarehouseOwnersAsync("WarehouseVoucher", "View", ct);
            return (IQueryable<T>)(object)rows.Where(t => products.Any(p => p.Id == t.WarehouseProductId) &&
                (!t.WarehouseVoucherId.HasValue || db.WarehouseVouchers.Any(v => v.Id == t.WarehouseVoucherId.Value && warehouses.Any(w => w.Id == v.WarehouseId) && voucherWarehouses.Any(w => w.Id == v.WarehouseId))));
        }
        var vouchers = (IQueryable<WarehouseVoucher>)(object)query;
        return (IQueryable<T>)(object)vouchers.Where(v => warehouses.Any(w => w.Id == v.WarehouseId) &&
            !v.WarehouseProductVouchers.Any(p => !products.Any(a => a.Id == p.WarehouseProductId && a.WarehouseId == v.WarehouseId)));
    }

    public async Task<IQueryable<T>> ApplyAsync<T>(IQueryable<T> query, string action, CancellationToken ct) where T : BaseEntity
    {
        var entity = typeof(T).Name;
        if (WarehouseEnforced && IsWarehouseEntity(entity))
            return await WarehouseScopeAsync(query, entity, action, ct);
        if (!IsEnforced(entity)) return query;
        var owners = await OwnersAsync(entity + "." + action, ct);
        if (owners.AllowedOwnerIds is { Length: 0 }) return query.Where(x => false);
        var allowed = owners.AllowedOwnerIds;
        var excluded = owners.ExcludedOwnerIds;
        if (typeof(T) == typeof(Contact))
        {
            var contacts = (IQueryable<Contact>)(object)query;
            var customers = await CustomersAsync(db.Customers.AsNoTracking().Where(c => !c.IsDeleted), "View", ct);
            // EntityId is the polymorphic parent. Require the owning customer even for an agent contact.
            contacts = contacts.Where(c => !c.IsDeleted && c.EntityId.HasValue &&
                ((c.ContactType == ContactTypeEnum.Customer && customers.Any(p => p.Id == c.EntityId.Value)) ||
                 (c.ContactType == ContactTypeEnum.Agent && db.Agents.Any(a => !a.IsDeleted && a.Id == c.EntityId.Value &&
                     a.CustomerId.HasValue && customers.Any(p => p.Id == a.CustomerId.Value)))));
            query = (IQueryable<T>)(object)contacts;
        }
        if (typeof(T) == typeof(Report))
        {
            var rows = (IQueryable<Report>)(object)query;
            var customers = await CustomersAsync(db.Customers.AsNoTracking(), "View", ct);
            var deals = await DealsAsync(db.Deals.AsNoTracking(), "View", ct);
            rows = rows.Where(r => (!r.CustomerId.HasValue || customers.Any(c => c.Id == r.CustomerId.Value))
                && (!r.DealId.HasValue || deals.Any(d => d.Id == r.DealId.Value)));
            if (IsEnforced("CallRecording"))
            {
                var calls = await ApplyAsync(db.CallRecordings.AsNoTracking(), "View", ct);
                rows = rows.Where(r => !r.CallRecordingId.HasValue || calls.Any(c => c.Id == r.CallRecordingId.Value));
            }
            query = (IQueryable<T>)(object)rows;
        }
        if (typeof(T) == typeof(Carizo))
        {
            var rows = (IQueryable<Carizo>)(object)query;
            var deals = await DealsAsync(db.Deals.AsNoTracking(), "View", ct);
            var reports = await ApplyAsync(db.Reports.AsNoTracking(), "View", ct);
            var deposits = await ApplyAsync(db.Accountancies.AsNoTracking(), "View", ct);
            rows = rows.Where(t => !t.EntityId.HasValue ||
                (t.EntityType == "Deal" && deals.Any(d => d.Id == t.EntityId.Value)) ||
                (t.EntityType == "Report" && reports.Any(r => r.Id == t.EntityId.Value)) ||
                (t.EntityType == "Acc" && deposits.Any(a => a.Id == t.EntityId.Value)));
            if (allowed != null) rows = rows.Where(x => allowed.Contains(x.AssignUserId));
            if (excluded.Length > 0) rows = rows.Where(x => !excluded.Contains(x.AssignUserId));
            return (IQueryable<T>)(object)rows;
        }
        if (typeof(T) == typeof(CallRecording))
        {
            var rows = (IQueryable<CallRecording>)(object)query;
            if (allowed != null) rows = rows.Where(x => allowed.Contains(x.DestinationUserId) || x.Participants.Any(p => allowed.Contains(p.UserId)));
            if (excluded.Length > 0) rows = rows.Where(x => !excluded.Contains(x.DestinationUserId) && !x.Participants.Any(p => excluded.Contains(p.UserId)));
            return (IQueryable<T>)(object)rows;
        }
        if (typeof(T) == typeof(Cashier))
        {
            var rows = (IQueryable<Cashier>)(object)query;
            if (allowed != null)
            {
                var ids = allowed.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                rows = rows.Where(x => ids.Contains(x.UserId));
            }
            if (excluded.Length > 0)
            {
                var ids = excluded.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToArray();
                rows = rows.Where(x => !ids.Contains(x.UserId));
            }
            return (IQueryable<T>)(object)rows;
        }
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
        if (WarehouseEnforced && IsWarehouseEntity(entity))
            return await CanAsync(entity, action, row.Id, ct);
        if (row is Carizo || row is CallRecording || row is Contact || row is Report)
            return await CanAsync(entity, action, row.Id, ct);
        if (row is Cashier cashier)
        {
            var owner = new string?[] { cashier.UserId.ToString() };
            return AllowsOwners(await OwnersAsync(entity + "." + action, ct), owner, owner);
        }
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
        if (!IsEnforced(entity) && !(WarehouseEnforced && IsWarehouseEntity(entity))) return true;
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
            "Transfer" => await CanRowAsync(db.Transfers.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Cashier" => await CanRowAsync(db.Cashiers.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Cash" => await CanRowAsync(db.Cashes.AsNoTracking().Where(x => x.Id == id), action, ct),
            "BaseCash" => await CanRowAsync(db.BaseCashes.AsNoTracking().Where(x => x.Id == id), action, ct),
            "BaseCenterCash" => await CanRowAsync(db.BaseCenterCashes.AsNoTracking().Where(x => x.Id == id), action, ct),
            "CashGroup" => await CanRowAsync(db.CashGroups.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Warehouse" => await CanRowAsync(db.Warehouses.AsNoTracking().Where(x => x.Id == id), action, ct),
            "WarehouseProduct" => await CanRowAsync(db.WarehouseProducts.AsNoTracking().Where(x => x.Id == id), action, ct),
            "WarehouseTransaction" => await CanRowAsync(db.WarehouseTransactions.AsNoTracking().Where(x => x.Id == id), action, ct),
            "WarehouseVoucher" => await CanRowAsync(db.WarehouseVouchers.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Contact" => await CanRowAsync(db.Contacts.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Contract" => await CanRowAsync(db.Contracts.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Carizo" => await CanRowAsync(db.Carizos.AsNoTracking().Where(x => x.Id == id), action, ct),
            "Report" => await CanRowAsync(db.Reports.AsNoTracking().Where(x => x.Id == id), action, ct),
            "CallRecording" => await CanRowAsync(db.CallRecordings.AsNoTracking().Where(x => x.Id == id), action, ct),
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
