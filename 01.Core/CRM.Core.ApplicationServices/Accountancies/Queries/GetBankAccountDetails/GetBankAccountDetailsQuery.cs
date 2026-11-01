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

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountDetails;
public sealed record GetBankAccountDetailsQuery(Guid Id) : IQuery<BankAccount?>;
public sealed class GetBankAccountDetailsQueryHandler(IAccountancyRepository repository) : IQueryHandler<GetBankAccountDetailsQuery, BankAccount?>
{
    public async Task<BankAccount?> HandleAsync(GetBankAccountDetailsQuery request, CancellationToken ct)
    {
        return await repository.GetBankAsync(request.Id, false, ct);
    }
}
