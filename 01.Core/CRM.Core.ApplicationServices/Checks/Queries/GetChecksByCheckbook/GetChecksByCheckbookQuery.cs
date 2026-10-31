using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Queries.GetChecksByCheckbook;
public sealed record CheckStatusOption(CheckStatus Value, string Text);
public sealed record CheckListResult(List<Check> Data, List<CheckStatusOption> Statuses);
public sealed record GetChecksByCheckbookQuery(Guid Id) : IQuery<CheckListResult>;
public sealed class GetChecksByCheckbookQueryHandler(ICheckRepository repository) : IQueryHandler<GetChecksByCheckbookQuery, CheckListResult>
{
    public async Task<CheckListResult> HandleAsync(GetChecksByCheckbookQuery request, CancellationToken ct)
    {
        return new(await repository.GetChecksAsync(request.Id, ct), Enum.GetValues<CheckStatus>().Select(s => new CheckStatusOption(s, s.ToString().Replace("_", " "))).ToList());
    }
}
