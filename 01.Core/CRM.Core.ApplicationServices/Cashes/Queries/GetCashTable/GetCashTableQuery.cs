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

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashTable;
public sealed record GetCashTableQuery(TableRequest Table, QueryFilterInput? Filter) : IQuery<CashListTable>;
public sealed class GetCashTableQueryHandler(ICashRepository repository, CurrentActor actor, IApplicationDates dates, ICashEffects effects) : IQueryHandler<GetCashTableQuery, CashListTable>
{
    public async Task<CashListTable> HandleAsync(GetCashTableQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return await repository.GetTableAsync(request.Table, QueryFilterResolver.Resolve(request.Filter, dates), current.User.Id, current.HasRole("مدیر ارشد", "مدیر مالی", "کارشناس مالی"), effects.ParseNumber(request.Table.Search), ct);
    }
}
