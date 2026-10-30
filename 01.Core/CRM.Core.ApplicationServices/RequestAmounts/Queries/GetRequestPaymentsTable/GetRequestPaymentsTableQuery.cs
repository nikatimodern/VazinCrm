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

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestPaymentsTable;
public sealed record GetRequestPaymentsTableQuery(Guid Id, TableRequest Table) : IQuery<object>;
public sealed class GetRequestPaymentsTableQueryHandler(IRequestAmountRepository repository, CurrentActor actor) : IQueryHandler<GetRequestPaymentsTableQuery, object>
{
    public async Task<object> HandleAsync(GetRequestPaymentsTableQuery request, CancellationToken ct)
    {
        var data = await repository.GetPaymentsTableAsync(request.Id, request.Table, ct);
        var current = await actor.GetAsync(ct);
        return new
        {
            data.Draw,
            data.RecordsTotal,
            data.RecordsFiltered,
            data.Data,
            hasAccess = current.HasRole("مدیر ارشد", "مدیر مالی", "کارشناس مالی")
        };
    }
}
