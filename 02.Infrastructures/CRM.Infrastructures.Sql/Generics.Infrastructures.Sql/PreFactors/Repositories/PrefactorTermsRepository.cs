using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.PreFactors.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.PreFactors.Repositories;

public sealed class PrefactorTermsRepository(CRMContextDb db) : IPrefactorTermsRepository
{
    private IQueryable<BaseDescription> Terms => db.BaseDescriptions
        .Where(x => x.Type == BaseDescriptionTypeEnum.PrefactorTerms);

    public Task<List<PrefactorTerm>> GetAsync(CancellationToken ct) => Terms.AsNoTracking()
        .OrderBy(x => x.Order).ThenBy(x => x.Id)
        .Select(x => new PrefactorTerm(x.Id, x.Order, x.Text))
        .ToListAsync(ct);

    public async Task<bool> SaveAsync(int? id, int order, string text, string userId, CancellationToken ct)
    {
        BaseDescription term;
        if (id.HasValue)
        {
            term = await Terms.SingleOrDefaultAsync(x => x.Id == id.Value, ct);
            if (term == null) return false;
        }
        else
        {
            term = new BaseDescription
            {
                Type = BaseDescriptionTypeEnum.PrefactorTerms,
                UserId = userId,
                Dir = "RTL"
            };
            db.BaseDescriptions.Add(term);
        }

        term.Order = order;
        term.Text = text;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReorderAsync(int[] originalIds, int[] orderedIds, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var terms = await Terms.OrderBy(x => x.Order).ThenBy(x => x.Id).ToListAsync(ct);
        if (!terms.Select(x => x.Id).SequenceEqual(originalIds) ||
            orderedIds.Length != terms.Count ||
            !orderedIds.ToHashSet().SetEquals(originalIds))
            return false;

        var byId = terms.ToDictionary(x => x.Id);
        for (var index = 0; index < orderedIds.Length; index++)
            byId[orderedIds[index]].Order = index + 1;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var term = await Terms.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (term == null) return false;
        db.BaseDescriptions.Remove(term);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
