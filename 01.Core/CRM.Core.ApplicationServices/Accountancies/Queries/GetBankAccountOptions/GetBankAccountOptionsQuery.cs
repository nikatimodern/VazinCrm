using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.ApplicationServices.Accountancies.Services;

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountOptions;
public sealed record GetBankAccountOptionsQuery(BankAccountTypeEnum? Type) : IQuery<object>;
public sealed class GetBankAccountOptionsQueryHandler(IAccountancyRepository repository) : IQueryHandler<GetBankAccountOptionsQuery, object>
{
    public async Task<object> HandleAsync(GetBankAccountOptionsQuery request, CancellationToken ct)
    {
        var items = await repository.GetBankAccountsAsync(request.Type, ct);
        return items.Select(f => new { value = f.Id, accountName = f.AccountName, bankName = f.BankName, accountNumber = f.AccountNumber, shebaNumber = f.ShebaNumber, cartNumber = f.CartNumber, officialType = f.OfficialType, type = f.BankAccountType }).ToList();
    }
}
