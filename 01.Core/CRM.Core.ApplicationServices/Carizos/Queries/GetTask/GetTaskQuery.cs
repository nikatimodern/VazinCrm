using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Queries.GetTask;
public sealed record GetTaskQuery(Guid Id) : IQuery<Carizo?>;
public sealed class GetTaskQueryHandler(ICarizoRepository repository) : IQueryHandler<GetTaskQuery, Carizo?>
{
    public async Task<Carizo?> HandleAsync(GetTaskQuery request, CancellationToken ct)
    {
        return await repository.GetAsync(request.Id, false, ct);
    }
}
