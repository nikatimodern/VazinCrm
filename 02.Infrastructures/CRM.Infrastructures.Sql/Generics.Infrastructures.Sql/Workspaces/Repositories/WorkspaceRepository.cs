#nullable enable
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Workspaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Workspaces.Repositories;
public sealed class WorkspaceRepository(CRMContextDb db) : IWorkspaceRepository
{
    public Task<ASetting?> GetSettingsAsync(CancellationToken ct) => db.ASettings.AsNoTracking().FirstOrDefaultAsync(ct);
}
