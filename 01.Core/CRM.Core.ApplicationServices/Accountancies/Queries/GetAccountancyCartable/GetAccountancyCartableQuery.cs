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

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyCartable;
public sealed record GetAccountancyCartableQuery(string? Search, CartableEnum Cartable, int Type, PayTypeEnum? PayType) : IQuery<object>;
public sealed class GetAccountancyCartableQueryHandler(IAccountancyRepository repository, CurrentActor actor, IAccountancyEffects effects) : IQueryHandler<GetAccountancyCartableQuery, object>
{
    public async Task<object> HandleAsync(GetAccountancyCartableQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var data = await repository.GetCartableAsync(request.Search, effects.ParseNumber(request.Search), request.Cartable, request.Type, request.PayType, ct);
        var model = new PaymentPartialData
        {
            Items = data.Items,
            PassType = request.Cartable == CartableEnum.کارشناس_تامین ? 56 : 55,
            UserId = current.User.Id
        };
        PaymentPermissions.Apply(model, current.Roles, current.User);
        return new
        {
            html = await effects.RenderPaymentsAsync(model, ct),
            count = data.Items.Count,
            filterCounts = data.FilterCounts
        };
    }
}
