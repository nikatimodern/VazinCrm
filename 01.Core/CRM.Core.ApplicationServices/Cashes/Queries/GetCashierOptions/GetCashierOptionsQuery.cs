using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashierOptions;
public sealed record GetCashierOptionsQuery() : IQuery<List<CashierOption>>;
public sealed class GetCashierOptionsQueryHandler(ICashRepository repository, CurrentActor actor) : IQueryHandler<GetCashierOptionsQuery, List<CashierOption>>
{
    public async Task<List<CashierOption>> HandleAsync(GetCashierOptionsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetCashierOptionsAsync(current.User.Id, ct);
    }
}
