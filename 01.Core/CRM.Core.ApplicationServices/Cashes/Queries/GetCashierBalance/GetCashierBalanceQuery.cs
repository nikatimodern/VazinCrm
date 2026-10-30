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

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashierBalance;
public sealed record GetCashierBalanceQuery() : IQuery<CashierBalanceDto?>;
public sealed class GetCashierBalanceQueryHandler(ICashRepository repository, CurrentActor actor) : IQueryHandler<GetCashierBalanceQuery, CashierBalanceDto?>
{
    public async Task<CashierBalanceDto?> HandleAsync(GetCashierBalanceQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        return current.HasRole("تنخواه دار") ? await repository.GetBalanceAsync(Guid.Parse(current.User.Id), ct) : null;
    }
}
