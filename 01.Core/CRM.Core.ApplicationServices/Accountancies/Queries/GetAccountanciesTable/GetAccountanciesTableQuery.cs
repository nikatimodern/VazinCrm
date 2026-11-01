using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.ApplicationServices.Accountancies.Services;

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountanciesTable;
public sealed record GetAccountanciesTableQuery(TableRequest Table, QueryFilterInput? Filter, int AccType, int PayType) : IQuery<AccountancyTable>;
public sealed class GetAccountanciesTableQueryHandler(IAccountancyRepository repository, CurrentActor actor, IApplicationDates dates) : IQueryHandler<GetAccountanciesTableQuery, AccountancyTable>
{
    public async Task<AccountancyTable> HandleAsync(GetAccountanciesTableQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var data = await repository.GetTableAsync(request.Table, QueryFilterResolver.Resolve(request.Filter, dates), request.AccType, request.PayType, current.User.Id, current.HasRole("مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی", "مدیر تامین"), ct);
        var row = request.Table.Start + 1;
        foreach (var acc in data.Data)
        {
            acc.Row = row++;
            acc.credit = acc.credit ?? acc.debit ?? acc.BackWard;
            if (acc.PayType is PayTypeEnum.اعتباری or PayTypeEnum.چکی && acc.Vosoled)
                acc.PayTypeTxt += " (وصول شده)";
        }

        return data;
    }
}
