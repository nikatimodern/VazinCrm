using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Queries.GetCheckbooks;
public sealed record GetCheckbooksQuery() : IQuery<List<CheckbookInfo>>;
public sealed class GetCheckbooksQueryHandler(ICheckRepository repository) : IQueryHandler<GetCheckbooksQuery, List<CheckbookInfo>>
{
    public async Task<List<CheckbookInfo>> HandleAsync(GetCheckbooksQuery request, CancellationToken ct)
    {
        return await repository.GetCheckbooksAsync(ct);
    }
}
