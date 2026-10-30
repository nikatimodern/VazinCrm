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

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashDetailedAccounts;
public sealed record GetCashDetailedAccountsQuery() : IQuery<List<CashOption<string>>>;
public sealed class GetCashDetailedAccountsQueryHandler(ICashExternalRepository external) : IQueryHandler<GetCashDetailedAccountsQuery, List<CashOption<string>>>
{
    public async Task<List<CashOption<string>>> HandleAsync(GetCashDetailedAccountsQuery request, CancellationToken ct)
    {
        var options = await external.GetDetailedAccountsAsync(1, ct);
        return options.Select(o => o with { Text = $"{o.Text} ({o.Value})" }).ToList();
    }
}
