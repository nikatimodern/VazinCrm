using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
public sealed record CartableFactorData(IList<Factor>? Factors, CRM.Core.Domain.ASettings.Entities.ASetting? Setting);
public sealed record GetCartableFactorsQuery(QueryFilterInput? Filter, Guid? CustomerId) : IQuery<CartableFactorData>;
public sealed class GetCartableFactorsQueryHandler(ICartableRepository repository, CurrentActor actor, IApplicationDates dates, ICartableEffects effects) : IQueryHandler<GetCartableFactorsQuery, CartableFactorData>
{
    public async Task<CartableFactorData> HandleAsync(GetCartableFactorsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var filter = QueryFilterResolver.Resolve(request.Filter, dates);
        var scope = new CartableReadScope(current.User.Id, current.HasRole("مدیر ارشد", "مدیر فروش", "مدیر مالی", "کارشناس مالی", "مشتری"), false);
        return new(await repository.GetFactorsAsync(filter, request.CustomerId, scope, effects.ParseNumber(filter.Search), ct), await repository.GetSettingsAsync(ct));
    }
}
