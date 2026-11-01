using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.Domain.Workspaces.Repositories;

namespace CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
public sealed record GetWorkspacePageQuery(string Module) : IQuery<WorkspacePageResult>;
public sealed record WorkspacePageResult(string Module, string UserId, string DisplayName, IList<string> Roles, bool SmallOffice, bool Sepidar, bool CrmFactors);
public sealed class GetWorkspacePageQueryHandler(CurrentActor actor, IWorkspaceRepository repository) : IQueryHandler<GetWorkspacePageQuery, WorkspacePageResult>
{
    public async Task<WorkspacePageResult> HandleAsync(GetWorkspacePageQuery query, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var settings = await repository.GetSettingsAsync(ct);
        return new(query.Module, current.User.Id, current.User.FullName, current.Roles, settings?.SmallOffice == true, settings?.Sepidar == true, settings?.CrmFactors == true);
    }
}
