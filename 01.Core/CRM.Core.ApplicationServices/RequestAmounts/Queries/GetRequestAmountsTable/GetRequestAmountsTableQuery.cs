using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountsTable;
public sealed record GetRequestAmountsTableQuery(TableRequest Table) : IQuery<RequestAmountsTable>;
public sealed class GetRequestAmountsTableQueryHandler(IRequestAmountRepository repository, IRequestAmountEffects effects) : IQueryHandler<GetRequestAmountsTableQuery, RequestAmountsTable>
{
    public async Task<RequestAmountsTable> HandleAsync(GetRequestAmountsTableQuery request, CancellationToken ct)
    {
        return await repository.GetTableAsync(request.Table, effects.ParseNumber(request.Table.Search), ct);
    }
}
