using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Queries.GetTaskList;
public sealed record GetTaskListQuery(int Type, int Location, int Systematic) : IQuery<object>;
public sealed class GetTaskListQueryHandler(ICarizoRepository repository, CurrentActor actor, ICarizoEffects effects, IDocumentReadAccess access) : IQueryHandler<GetTaskListQuery, object>
{
    public async Task<object> HandleAsync(GetTaskListQuery request, CancellationToken ct)
    {
        if (request.Location == 2 && access.IsEnforced("Dashboard") &&
            (!await access.HasAsync("Dashboard", "View", ct) || !await access.HasAsync("Dashboard", "ViewTasks", ct)))
            throw new UnauthorizedAccessException();
        var user = (await actor.GetAsync(ct)).User;
        var tasks = await repository.GetTasksAsync(user.Id, request.Type, request.Systematic, DateTime.Now, ct);
        return await effects.RenderAsync(tasks, user, request.Type, request.Location, false, ct);
    }
}
