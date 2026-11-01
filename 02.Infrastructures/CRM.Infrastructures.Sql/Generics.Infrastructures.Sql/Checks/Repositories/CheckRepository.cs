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
public sealed class CheckRepository(CRMContextDb db) : ICheckRepository
{
    public Task<List<CheckbookInfo>> GetCheckbooksAsync(CancellationToken ct) => db.Checkbooks.AsNoTracking().Include(c => c.BankAccount).Select(c => new CheckbookInfo(c, c.Checks.Count)).ToListAsync(ct);
    public Task<List<Check>> GetChecksAsync(Guid id, CancellationToken ct) => db.Checks.AsNoTracking().Where(c => c.CheckbookId == id).OrderByDescending(c => c.CreateDate).ToListAsync(ct);
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
