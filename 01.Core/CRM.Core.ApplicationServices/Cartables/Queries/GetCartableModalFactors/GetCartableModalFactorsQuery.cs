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
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactors;
public sealed record GetCartableModalFactorsQuery(Guid Id, int Type) : IQuery<CartableFactorData>;
public sealed class GetCartableModalFactorsQueryHandler(ICartableRepository repository) : IQueryHandler<GetCartableModalFactorsQuery, CartableFactorData>
{
    public async Task<CartableFactorData> HandleAsync(GetCartableModalFactorsQuery request, CancellationToken ct)
    {
        return new(await repository.GetModalFactorsAsync(request.Id, request.Type, ct), await repository.GetSettingsAsync(ct));
    }
}
