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

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountsTable;
public sealed record GetBankAccountsTableQuery(TableRequest Table, int? Type) : IQuery<BankAccountTable>;
public sealed class GetBankAccountsTableQueryHandler(IAccountancyRepository repository) : IQueryHandler<GetBankAccountsTableQuery, BankAccountTable>
{
    public async Task<BankAccountTable> HandleAsync(GetBankAccountsTableQuery request, CancellationToken ct)
    {
        return await repository.GetBankTableAsync(request.Table, request.Type, ct);
    }
}
