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

namespace CRM.Core.ApplicationServices.Cashes.Queries.GetCashierPage;
public sealed record CashierSoftwareData(ExternalSoftware ExternalSoftware, List<CashOption<string>> SelectOptions);
public sealed record GetCashierPageQuery() : IQuery<List<CashierSoftwareData>>;
public sealed class GetCashierPageQueryHandler(ICashExternalRepository external, ICashEffects effects) : IQueryHandler<GetCashierPageQuery, List<CashierSoftwareData>>
{
    public async Task<List<CashierSoftwareData>> HandleAsync(GetCashierPageQuery request, CancellationToken ct)
    {
        var result = new List<CashierSoftwareData>();
        foreach (var software in await external.GetAccountingSoftwareAsync(ct))
        {
            var options = await external.GetDetailedAccountsAsync(software.Code, ct);
            result.Add(new(software, options.Select(o => o with { Text = $"{effects.Normalize(o.Text)} ({o.Value})" }).ToList()));
        }

        return result;
    }
}
