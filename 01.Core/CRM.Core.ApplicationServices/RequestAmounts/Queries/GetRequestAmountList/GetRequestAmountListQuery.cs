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

namespace CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountList;
public sealed record GetRequestAmountListQuery(int Type, string? Search, Guid? OrderHeadId) : IQuery<object>;
public sealed class GetRequestAmountListQueryHandler(IRequestAmountRepository repository, CurrentActor actor, IRequestAmountEffects effects) : IQueryHandler<GetRequestAmountListQuery, object>
{
    public async Task<object> HandleAsync(GetRequestAmountListQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var all = current.HasRole("مدیر ارشد", "مدیر مالی", "کارشناس مالی", "مدیر عامل", "مدیر تامین");
        var data = await repository.GetListAsync(request.Type, request.Search, effects.ParseNumber(request.Search), request.OrderHeadId, current.User.Id, all, ct);
        return await effects.RenderAsync(data, ct);
    }
}
