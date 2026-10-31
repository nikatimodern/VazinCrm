#nullable enable
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;

namespace CRM.Core.Domain.Workspaces.Repositories;
public interface IWorkspaceRepository
{
    Task<ASetting?> GetSettingsAsync(CancellationToken cancellationToken);
}
