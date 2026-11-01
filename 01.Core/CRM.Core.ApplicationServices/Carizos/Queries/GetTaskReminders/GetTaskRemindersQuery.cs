using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Queries.GetTaskReminders;
public sealed record GetTaskRemindersQuery() : IQuery<object>;
public sealed class GetTaskRemindersQueryHandler(ICarizoRepository repository, CurrentActor actor, IApplicationDates dates, ICarizoEffects effects) : IQueryHandler<GetTaskRemindersQuery, object>
{
    public async Task<object> HandleAsync(GetTaskRemindersQuery request, CancellationToken ct)
    {
        var user = (await actor.GetAsync(ct)).User;
        var tasks = await repository.GetRemindersAsync(user.Id, DateTime.Now, ct);
        foreach (var task in tasks)
            task.ElapsTime = dates.Elapsed(task.DateMiladi!.Value);
        return await effects.RenderAsync(tasks, user, 0, 0, true, ct);
    }
}
