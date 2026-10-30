using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Queries.GetCheck;
public sealed record GetCheckQuery(Guid Id) : IQuery<Check?>;
public sealed class GetCheckQueryHandler(ICheckRepository repository) : IQueryHandler<GetCheckQuery, Check?>
{
    public async Task<Check?> HandleAsync(GetCheckQuery request, CancellationToken ct)
    {
        return await repository.GetAsync(request.Id, false, ct);
    }
}
