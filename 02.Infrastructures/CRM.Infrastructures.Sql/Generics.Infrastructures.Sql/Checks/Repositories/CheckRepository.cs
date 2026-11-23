#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Checks.Repositories;
public sealed class CheckRepository(CRMContextDb db, CRM.Core.Domain.AccessCatalog.Repositories.IDocumentReadAccess access) : ICheckRepository
{
    public async Task<List<CheckbookInfo>> GetCheckbooksAsync(CancellationToken ct)
    {
        var books = await access.ApplyAsync(db.Checkbooks.AsNoTracking(), "View", ct);
        var checks = await access.ApplyAsync(db.Checks.AsNoTracking(), "View", ct);
        var accounts = await access.ApplyAsync(db.BankAccounts.AsNoTracking(), "View", ct);
        return await books.Where(b => accounts.Any(a => a.Id == b.BankAccountId)).Include(c => c.BankAccount)
            .Select(c => new CheckbookInfo(c, checks.Count(x => x.CheckbookId == c.Id))).ToListAsync(ct);
    }
    public async Task<List<Check>> GetChecksAsync(Guid id, CancellationToken ct) => await (await access.ApplyAsync(db.Checks.AsNoTracking(), "View", ct)).Where(c => c.CheckbookId == id).OrderByDescending(c => c.CreateDate).ToListAsync(ct);
    public Task<Check?> GetAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Checks : db.Checks.AsNoTracking()).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<Checkbook?> GetCheckbookAsync(Guid id, CancellationToken ct) => db.Checkbooks.Include(c => c.Checks).FirstOrDefaultAsync(c => c.Id == id, ct);
    public void AddCheckbook(Checkbook checkbook) => db.Checkbooks.Add(checkbook);
    public async Task SaveCheckAsync(Check check, bool create, CancellationToken ct)
    {
        if (create)
            db.Checks.Add(check);
        else
        {
            var current = await db.Checks.FirstAsync(c => c.Id == check.Id, ct);
            db.Entry(current).CurrentValues.SetValues(check);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
