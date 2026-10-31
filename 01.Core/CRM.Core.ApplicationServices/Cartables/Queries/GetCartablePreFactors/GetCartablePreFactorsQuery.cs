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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactors;
public sealed record GetCartablePreFactorsQuery(QueryFilterInput? Filter) : IQuery<CartablePreFactors>;
public sealed class GetCartablePreFactorsQueryHandler(ICartableRepository repository, CurrentActor actor, IApplicationDates dates, ICartableEffects effects) : IQueryHandler<GetCartablePreFactorsQuery, CartablePreFactors>
{
    public async Task<CartablePreFactors> HandleAsync(GetCartablePreFactorsQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var filter = QueryFilterResolver.Resolve(request.Filter, dates);
        var scope = new CartableReadScope(current.User.Id, current.HasRole("مدیر ارشد", "مدیر فروش", "مدیر مالی", "کارشناس مالی"), current.HasRole("مدیر ارشد", "مدیر تامین"));
        var factors = await repository.GetPreFactorsAsync(filter, scope, effects.ParseNumber(filter.Search), ct);
        await effects.FillPreFactorDetailsAsync(factors, ct);
        return new(factors, await repository.GetSettingsAsync(ct));
    }
}
