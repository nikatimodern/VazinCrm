#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Repositories;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using CRM.Core.Domain.Accountancies.Enum;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace CRM.Infrastructures.Sql.Customers.Repositories;

public sealed class CustomerRepository(CRMContextDb db, UserContextDb users) : ICustomerRepository, IAsyncDisposable
{
    private IDbContextTransaction? transaction;
    public Task<Customer?> GetAsync(Guid id, bool includeDeleted, bool includeRelations, CancellationToken ct)
    {
        IQueryable<Customer> query = db.Customers.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        if (includeRelations) query = query.Include(c => c.UserCustomers);
        return query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }
    public Task<Customer?> GetByMobileAsync(string mobile, CancellationToken ct) => db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Mobile == mobile, ct);
    public Task<List<CustomerOption>> SearchOptionsAsync(string search, int? roleType, CancellationToken ct) => db.Customers.AsNoTracking()
        .Where(c => !roleType.HasValue || (int)c.RoleType == roleType.Value || (int)c.RoleType == 3)
        .Where(c => EF.Functions.Like(c.AgentName, "%" + search + "%") || EF.Functions.Like(c.CompanyName, "%" + search + "%") || EF.Functions.Like(c.CustomerCode.ToString(), "%" + search + "%"))
        .Select(c => new CustomerOption(c.Id, c.CompanyName + " " + c.AgentName + " کد مشتری: " + c.CustomerCode)).Take(20).ToListAsync(ct);
    public Task<List<CustomerSupplier>> GetSuppliersAsync(CancellationToken ct) => db.Customers.AsNoTracking().Where(c => (int)c.RoleType == 2 || (int)c.RoleType == 3)
        .Select(c => new CustomerSupplier(c.Id, c.CompanyName, c.AgentName, c.CustomerCode)).ToListAsync(ct);
    public async Task<List<Customer>> GetRelatedAsync(Guid id, CancellationToken ct)
    {
        var model = await GetAsync(id, false, false, ct); if (model == null) return [];
        var query = db.Customers.AsNoTracking();
        query = model.Juridical == 1 ? query.Where(c => c.Id != id && ((c.ShenaseMeli == model.ShenaseMeli && model.ShenaseMeli != null) ||
            (c.EghtesadiCode == model.EghtesadiCode && model.EghtesadiCode != null) || (c.NumberRegister == model.NumberRegister && model.NumberRegister != null))) :
            query.Where(c => EF.Functions.Like(c.AgentName, "%" + model.AgentName + "%") || (c.Mobile == model.Mobile && model.Mobile != null && c.Id != id));
        return await query.ToListAsync(ct);
    }
    public Task<List<Customer>> GetDuplicatesAsync(Customer model, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking();
        query = model.Juridical == 1 ? query.Where(c => c.AgentName == model.AgentName || c.CompanyName == model.CompanyName || c.ShenaseMeli == model.ShenaseMeli || c.EghtesadiCode == model.EghtesadiCode || c.NumberRegister == model.NumberRegister) :
            query.Where(c => EF.Functions.Like(c.AgentName, "%" + model.AgentName + "%") || c.Mobile == model.Mobile);
        return query.ToListAsync(ct);
    }
    public Task<bool> HasLegalDuplicateAsync(Customer model, CancellationToken ct) => db.Customers.AnyAsync(c =>
        (!string.IsNullOrEmpty(model.CompanyName) && c.CompanyName == model.CompanyName) || (!string.IsNullOrEmpty(model.EghtesadiCode) && c.EghtesadiCode == model.EghtesadiCode) ||
        (!string.IsNullOrEmpty(model.NumberRegister) && c.NumberRegister == model.NumberRegister) || (!string.IsNullOrEmpty(model.ShenaseMeli) && c.ShenaseMeli == model.ShenaseMeli), ct);
    public Task<bool> HasAssignmentAsync(Guid customerId, Guid userId, CancellationToken ct) => db.UserCustomers.AnyAsync(c => c.CustomerId == customerId && c.UserId == userId, ct);
    public async Task RemoveAssignmentAsync(Guid customerId, Guid userId, CancellationToken ct)
    { var link = await db.UserCustomers.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.UserId == userId, ct); if (link != null) db.UserCustomers.Remove(link); }
    public async Task AddAssignmentsAsync(Guid customerId, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        var existing = await db.UserCustomers.Where(c => c.CustomerId == customerId && userIds.Contains(c.UserId)).Select(c => c.UserId).ToListAsync(ct);
        foreach (var id in userIds.Distinct().Except(existing)) db.UserCustomers.Add(new UserCustomer { CustomerId = customerId, UserId = id });
    }
    public async Task<List<AppUser>> GetAssignedUsersAsync(Guid customerId, CancellationToken ct)
    { var ids = await db.UserCustomers.AsNoTracking().Where(c => c.CustomerId == customerId).Select(c => c.UserId.ToString()).ToListAsync(ct); return await users.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync(ct); }
    public async Task<string> GetUserNamesAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    { var ids = userIds.Select(id => id.ToString()).ToArray(); var names = await users.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.FirstName, u.LastName }).ToListAsync(ct); return string.Join(" _ ", names.Select(u => (u.FirstName + " " + u.LastName).Trim())); }
    public async Task<List<Customer>> GetListAsync(CustomerListFilter filter, string userId, bool readAll, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().Include(c => c.UserCustomers).AsQueryable();
        if (!readAll) { var id = Guid.Parse(userId); query = query.Where(c => db.UserCustomers.Any(u => u.CustomerId == c.Id && u.UserId == id)); }
        if (filter.RoleType == 1) query = query.Where(c => (int)c.RoleType == 1 || (int)c.RoleType == 3);
        if (filter.RoleType == 2) query = query.Where(c => (int)c.RoleType == 2 || (int)c.RoleType == 3);
        if (filter.Juridical != -1) query = query.Where(c => c.Juridical == filter.Juridical);
        if (filter.Search != null)
        {
            var val = filter.Search; var keywords = filter.Keywords ?? []; query = query.Where(c => keywords.Any(k => c.NormalizedCompanyName.Contains(k) || c.NormalizedAgentName.Contains(k)) || c.CustomerCode.ToString() == val)
            .OrderByDescending(c => (c.NormalizedCompanyName == val ? 100 : 0) + (c.NormalizedAgentName == val ? 100 : 0) + (c.NormalizedCompanyName.StartsWith(val) ? 50 : 0) + (c.NormalizedAgentName.StartsWith(val) ? 50 : 0) + keywords.Count(k => c.NormalizedAgentName.Contains(k) || c.NormalizedCompanyName.Contains(k)) * 10).ThenByDescending(c => c.ModifyDate);
        }
        else query = query.OrderBy(c => c.Accepted).ThenByDescending(c => c.ModifyDate).Skip(filter.Skip).Take(filter.Take);
        var data = await query.ToListAsync(ct);
        var ids = data.SelectMany(c => c.UserCustomers).Select(u => u.UserId.ToString()).Distinct().ToArray();
        var names = await users.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.FirstName, u.LastName }).ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), ct);
        foreach (var customer in data) customer.SelExpertRel = string.Join(" _ ", customer.UserCustomers.Select(u => u.UserId.ToString()).Distinct().Where(names.ContainsKey).Select(id => names[id]));
        return data;
    }
    public async Task<CustomerTableResult> GetTableAsync(CustomerTableRequest filter, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().Where(c => c.Juridical == filter.Juridical);
        if (!string.IsNullOrEmpty(filter.Search)) query = query.Where(c => c.AgentName.Contains(filter.Search));
        var count = await query.CountAsync(ct);
        query = filter.SortColumn == "AgentName" ? (filter.Ascending ? query.OrderBy(c => c.AgentName) : query.OrderByDescending(c => c.AgentName)) :
            (filter.Ascending ? query.OrderBy(c => c.CreateDate) : query.OrderByDescending(c => c.CreateDate));
        return new(filter.Draw, count, count, await query.Skip(filter.Skip).Take(filter.Take).ToListAsync(ct));
    }
    public Task<List<CustomerSummary>> GetSummariesAsync(CancellationToken ct) => db.Customers.AsNoTracking().Select(c => new CustomerSummary(c.Id, c.Mobile, c.Tel, (int)c.RoleType, (c.CompanyName + " " + c.AgentName).Trim(), c.CustomerCode, c.CompanyName, c.AgentName)).ToListAsync(ct);
    public Task<List<CustomerPhone>> GetPhonebookAsync(string search, string[] keywords, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (string.IsNullOrWhiteSpace(search)) query = query.OrderByDescending(c => c.ModifyDate).Take(50);
        else query = query.Where(c => keywords.Any(k => c.NormalizedCompanyName.Contains(k) || c.NormalizedAgentName.Contains(k) || c.Tel.Contains(search) || c.Mobile.Contains(search)) || c.CustomerCode.ToString() == search)
            .OrderByDescending(c => (c.NormalizedCompanyName == search ? 100 : 0) + (c.NormalizedAgentName == search ? 100 : 0) + (c.NormalizedCompanyName.StartsWith(search) ? 50 : 0) + (c.NormalizedAgentName.StartsWith(search) ? 50 : 0) + keywords.Count(k => c.NormalizedAgentName.Contains(k) || c.NormalizedCompanyName.Contains(k)) * 10).ThenByDescending(c => c.ModifyDate);
        return query.Select(c => new CustomerPhone(c.Id, c.CompanyName + " " + c.AgentName, c.Tel, c.CustomerCode, c.Mobile, c.ModifyDate, c.NormalizedAgentName, c.NormalizedCompanyName)).ToListAsync(ct);
    }
    public Task<List<Customer>> GetForNormalizationAsync(CancellationToken ct) => db.Customers.ToListAsync(ct);
    public Task<ASetting?> GetSettingsAsync(CancellationToken ct) => db.ASettings.AsNoTracking().FirstOrDefaultAsync(ct);
    public async Task<CustomerStatus> GetStatusAsync(Guid id, CancellationToken ct) => new(await db.Factors.AnyAsync(f => f.CustomerId == id && f.status == FactorStatus.تایید_شده, ct), await db.Factors.AnyAsync(f => f.CustomerId == id && f.Export == true, ct));
    public async Task DeleteWithPreFactorsAsync(Customer model, CancellationToken ct)
    {
        var customer = await db.Customers.Include(c => c.PreFactors).ThenInclude(p => p.Factors).FirstOrDefaultAsync(c => c.Id == model.Id, ct);
        if (customer == null) return;
        void Stamp(CRM.Framework.Domain.BaseEntity entity) { entity.IsDeleted = true; entity.UserModify = model.UserModify; entity.UserModifyID = model.UserModifyID; entity.ModifyDate = model.ModifyDate; entity.ModifyDateTxt = model.ModifyDateTxt; }
        Stamp(customer); foreach (var p in customer.PreFactors ?? []) { Stamp(p); foreach (var f in p.Factors ?? []) Stamp(f); }
    }
    public async Task UpdateAsync(Customer model, CancellationToken ct)
    { var tracked = await db.Customers.FirstAsync(c => c.Id == model.Id, ct); db.Entry(tracked).CurrentValues.SetValues(model); }
    public async Task AddAsync(Customer model, Guid userId, CancellationToken ct)
    { db.Customers.Add(model); db.UserCustomers.Add(new UserCustomer { CustomerId = model.Id, UserId = userId }); await db.SaveChangesAsync(ct); }
    public async Task SyncDealsAsync(Customer model, CancellationToken ct)
    {
        var deals = await db.Deals.Where(d => d.CustomerId == model.Id).ToListAsync(ct); var ids = deals.Select(d => d.Id).ToArray();
        var name = $"{model.CompanyName} {model.AgentName}".Trim();
        foreach (var d in deals)
        {
            d.CustomerName = name; d.MobileNumber = model.Mobile; d.CusCompanyName = model.CompanyName; d.CusNumberRegister = model.NumberRegister; d.CusEghtesadiCode = model.EghtesadiCode;
            d.CusShenaseMeli = model.ShenaseMeli; d.CusMelliCode = model.AgentMeliCode; d.CusPostalCode = model.PostalCode; d.CusJuridical = model.Juridical; d.CusAddress = model.Address; d.CusTel = model.Tel;
            d.CusSex = model.Sex; d.CusProvince = model.Province; d.CusCity = model.City; d.CusWorkSenf = model.WorkSenf; d.CusWorkSenfTxt = model.WorkSenfTxt; d.CusWorkSenfDescription = model.WorkSenfDescription;
            d.CusAcquaintanceWay = model.AcquaintanceWay; d.CusAcquaintanceWayTxt = model.AcquaintanceWayTxt;
        }
        foreach (var p in await db.PreFactors.Where(p => p.DealId.HasValue && ids.Contains(p.DealId.Value)).ToListAsync(ct)) { p.CustomerId = model.Id; p.CustomerCode = model.CustomerCode; p.Customer = model.CompanyName; p.AgentName = model.AgentName; p.Cus_Mobile = model.Mobile; }
        foreach (var f in await db.Factors.Where(f => f.DealId.HasValue && ids.Contains(f.DealId.Value)).ToListAsync(ct)) { f.CustomerId = model.Id; f.CustomerCode = model.CustomerCode; f.Customer = model.CompanyName; f.AgentName = model.AgentName; f.Cus_Mobile = model.Mobile; }
        foreach (var r in await db.Reports.Where(r => r.DealId.HasValue && ids.Contains(r.DealId.Value)).ToListAsync(ct)) { r.CustomerId = model.Id; r.CustomerCode = model.CustomerCode; r.CustomerTxt = $"{name}(کد مشتری {model.CustomerCode})"; r.RepTel = model.Mobile; }
        foreach (var o in await db.OrderHeads.Where(o => o.DealId.HasValue && ids.Contains(o.DealId.Value)).ToListAsync(ct)) { o.Customer = model.Id; o.CustomerCode = model.CustomerCode; o.CustomerTxt = name; }
        foreach (var a in await db.Accountancies.Where(a => a.DealId.HasValue && ids.Contains(a.DealId.Value)).ToListAsync(ct)) { a.CustomerId = model.Id; a.CustomerCode = model.CustomerCode; a.CustomerName = name; }
    }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task BeginWriteAsync(CancellationToken ct) { transaction = await db.Database.BeginTransactionAsync(ct); }
    public async Task CommitAsync(CancellationToken ct) { if (transaction != null) await transaction.CommitAsync(ct); }
    public async ValueTask DisposeAsync() { if (transaction != null) await transaction.DisposeAsync(); }
    public async Task<List<PreFactor>> GetPreFactorsAsync(Guid? customerId, Guid? dealId, bool includeDeleted, CancellationToken ct)
    {
        if (!customerId.HasValue && !dealId.HasValue) return [];
        IQueryable<PreFactor> query = db.PreFactors.AsNoTracking().Include(p => p.Deal).Include(p => p.CustomerEntity);
        if (includeDeleted) query = query.IgnoreQueryFilters();
        query = dealId.HasValue ? query.Where(p => p.DealId == dealId) : query.Where(p => p.CustomerId == customerId);
        var data = await query.OrderByDescending(p => p.DateMiladi).ThenByDescending(p => p.CreateDate).ToListAsync(ct);
        var ids = data.Select(p => p.Id).ToArray();
        var amounts = await db.Accountancies.AsNoTracking().ForPreFactor().NotVosoled().Where(a => a.pfid.HasValue && ids.Contains(a.pfid.Value)).GroupBy(a => a.pfid)
            .Select(g => new { Id = g.Key!.Value, Amount = g.Sum(a => a.AccType == AcEnum.واریزی ? a.credit : -a.BackWard) ?? 0 }).ToDictionaryAsync(g => g.Id, g => g.Amount, ct);
        var settings = await GetSettingsAsync(ct); var counts = new Dictionary<Guid, int>();
        if (settings?.CrmFactors == true) counts = await db.Factors.AsNoTracking().Where(f => ids.Contains(f.PfactorID)).GroupBy(f => f.PfactorID).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Id, g => g.Count, ct);
        foreach (var p in data) { p.ReciveAmount = amounts.GetValueOrDefault(p.Id); p.PaymentAmount = p.SumTotalAmount + p.TotalTax - p.Discount - p.ReciveAmount; p.SumTotalAmountTax = p.SumTotalAmount + p.TotalTax - p.Discount; if (settings?.CrmFactors == true) p.CountFactor = counts.GetValueOrDefault(p.Id); }
        return data;
    }
    public Task<PreFactor?> GetPreFactorAsync(Guid id, CancellationToken ct) => db.PreFactors.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task ChangePreFactorUsersAsync(Guid pfId, AppUser? salesUser, AppUser? supplyUser, CancellationToken ct)
    {
        var pf = await db.PreFactors.FirstAsync(p => p.Id == pfId, ct);
        if (supplyUser != null) { pf.UserTaminId = Guid.Parse(supplyUser.Id); pf.UserTaminTxt = supplyUser.FullName; }
        if (salesUser != null) { pf.UserCreatorID = salesUser.Id; pf.UserCreator = salesUser.FullName; foreach (var factor in await db.Factors.Where(f => f.PfactorID == pfId).ToListAsync(ct)) { factor.SelerId = salesUser.Id; factor.SelerFullName = salesUser.FullName; } }
    }
}
