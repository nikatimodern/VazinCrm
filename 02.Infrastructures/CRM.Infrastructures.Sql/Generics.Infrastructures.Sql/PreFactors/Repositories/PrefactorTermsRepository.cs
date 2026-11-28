using System;
using System.Collections.Generic;
using Generics.Infrastructures.Sql;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.PreFactors.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.PreFactors.Repositories;

public sealed class PrefactorTermsRepository(CRMContextDb db, UserContextDb userDb) : IPrefactorTermsRepository
{
    private IQueryable<BaseDescription> Terms => db.BaseDescriptions
        .Where(x => x.Type == BaseDescriptionTypeEnum.PrefactorTerms);

    public async Task<List<PrefactorTerm>> GetAsync(CancellationToken ct)
    {
        var terms = await Terms.AsNoTracking()
            .OrderBy(x => x.Order).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Order, x.Text, x.UserId, x.CreateDate })
            .ToListAsync(ct);

        var userIds = terms.Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var users = await userDb.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName })
            .ToListAsync(ct);

        var names = users.ToDictionary(x => x.Id, x =>
        {
            var name = string.Join(" ", new[] { x.FirstName, x.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            return string.IsNullOrWhiteSpace(name) ? x.UserName : name;
        });

        return terms.Select(x => new PrefactorTerm(x.Id, x.Order, x.Text, x.UserId, x.CreateDate,
            x.UserId != null && names.TryGetValue(x.UserId, out var name) ? name : null)).ToList();
    }

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
                CreateDate = DateTime.Now,
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
