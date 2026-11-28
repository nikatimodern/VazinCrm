using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Ai.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Ai.Repositories;

public sealed class AiCatalogReadRepository(CRMContextDb db) : IAiCatalogReadRepository
{
    public async Task<IReadOnlyList<AiProviderSummary>> GetProvidersAsync(int offset, int limit, CancellationToken ct) =>
        await db.AiProviders.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.Key).ThenBy(x => x.Id)
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new AiProviderSummary(x.Id, x.Key, x.DisplayName, x.IsEnabled)).ToListAsync(ct);

    public async Task<IReadOnlyList<AiModelSummary>> GetModelsAsync(Guid providerId, int offset, int limit, CancellationToken ct) =>
        await (from model in db.AiModels.AsNoTracking()
               join provider in db.AiProviders.AsNoTracking() on model.ProviderId equals provider.Id
               where model.ProviderId == providerId && !model.IsDeleted && !provider.IsDeleted
               orderby model.ModelKey, model.Id
               select new AiModelSummary(model.Id, model.ProviderId, model.ModelKey, model.DisplayName, model.IsEnabled, model.VerifiedAtUtc))
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);

    public async Task<AiRouteSummary> GetRouteAsync(string taskKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(taskKey) || taskKey.Length > 96) return null;
        return await (from route in db.AiTaskRoutes.AsNoTracking()
                      join model in db.AiModels.AsNoTracking() on route.PrimaryModelId equals model.Id
                      join provider in db.AiProviders.AsNoTracking() on model.ProviderId equals provider.Id
                      where route.TaskKey == taskKey && !route.IsDeleted && !model.IsDeleted && !provider.IsDeleted
                      select new AiRouteSummary(route.TaskKey, route.PrimaryModelId, route.TimeoutMs, route.MaxOutputTokens,
                          route.IsEnabled && model.IsEnabled && provider.IsEnabled))
            .SingleOrDefaultAsync(ct);
    }
}
