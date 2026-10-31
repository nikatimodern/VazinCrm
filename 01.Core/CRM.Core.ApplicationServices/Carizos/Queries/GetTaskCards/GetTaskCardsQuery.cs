using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;

namespace CRM.Core.ApplicationServices.Carizos.Queries.GetTaskCards;
public sealed record TaskCardResult(Guid Id, int Column, string Html);
public sealed record TaskCardsResult(Guid Id, Guid GroupId, List<TaskCardResult> Cards);
public sealed record GetTaskCardsQuery(Guid Id, int Systematic = 0, int Location = 1) : IQuery<TaskCardsResult>;
public sealed class GetTaskCardsQueryHandler(ICarizoRepository repository, CurrentActor actor, ICarizoEffects effects) : IQueryHandler<GetTaskCardsQuery, TaskCardsResult>
{
    public async Task<TaskCardsResult> HandleAsync(GetTaskCardsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var changes = await repository.GetCardChangesAsync(request.Id, current.User.Id, ct);
        var now = DateTime.Now.Date;
        var cards = new List<TaskCardResult>();
        foreach (var task in changes.Tasks)
        {
            if (request.Systematic == 1 && !task.Systematic || request.Systematic == 2 && task.Systematic)
                continue;
            var column = task.TaskStatus ? (task.TaskHide ? 3 : 2) : task.DateMiladi.HasValue && task.DateMiladi.Value.Date <= now ? 1 : 0;
            if (request.Location == 2 && column != 1)
                continue;
            cards.Add(new(task.Id, column, await effects.RenderCardsAsync([task], current.User, column, request.Location, ct)));
        }

        return new(request.Id, changes.GroupId, cards);
    }
}
