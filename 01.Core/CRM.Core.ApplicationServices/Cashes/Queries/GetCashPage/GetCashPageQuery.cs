using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashPage;
public sealed record CashPageData(bool IsTaminUser, List<CashOption<Guid>> CashCenters, List<CashOption<Guid>> CashTitles, List<CashOption<Guid>> CashierTitles);
public sealed record GetCashPageQuery() : IQuery<CashPageData>;
public sealed class GetCashPageQueryHandler(ICashRepository repository, CurrentActor actor) : IQueryHandler<GetCashPageQuery, CashPageData>
{
    public async Task<CashPageData> HandleAsync(GetCashPageQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var supply = current.HasRole("مدیر تامین", "کارشناس تامین");
        return new(supply, await repository.GetCentersAsync(ct), await repository.GetTitlesAsync(supply, ct), await repository.GetOwnCashiersAsync(current.User.Id, ct));
    }
}
