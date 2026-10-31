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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;
public sealed record CartableSupplyResult(CartableSupplyData Data, CRM.Core.Domain.ASettings.Entities.ASetting? Setting, string UserId, bool IsAdmin);
public sealed record GetCartableSupplyQuery(string? Search, int Type) : IQuery<CartableSupplyResult>;
public sealed class GetCartableSupplyQueryHandler(ICartableRepository repository, CurrentActor actor, ICartableEffects effects) : IQueryHandler<GetCartableSupplyQuery, CartableSupplyResult>
{
    public async Task<CartableSupplyResult> HandleAsync(GetCartableSupplyQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var admin = current.HasRole("مدیر ارشد", "مدیر تامین");
        return new(await repository.GetSupplyAsync(request.Search, request.Type, new(current.User.Id, false, admin), effects.ParseNumber(request.Search), ct), await repository.GetSettingsAsync(ct), current.User.Id, admin);
    }
}
