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

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashGroupTable;
public sealed record GetCashGroupTableQuery(TableRequest Table) : IQuery<CashTable<CashGroupRow>>;
public sealed class GetCashGroupTableQueryHandler(ICashRepository repository, CurrentActor actor, ICashEffects effects) : IQueryHandler<GetCashGroupTableQuery, CashTable<CashGroupRow>>
{
    public async Task<CashTable<CashGroupRow>> HandleAsync(GetCashGroupTableQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetGroupTableAsync(request.Table, current.User.Id, current.HasRole("مدیر ارشد", "مدیر مالی", "کارشناس مالی"), effects.ParseNumber(request.Table.Search), ct);
    }
}
