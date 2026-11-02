using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Repositories;

namespace CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyTransfer;

public sealed record GetAccountancyTransferQuery(Guid AccountancyId) : IQuery<AccountancyTransfer?>;

public sealed class GetAccountancyTransferQueryHandler(IAccountancyRepository repository) : IQueryHandler<GetAccountancyTransferQuery, AccountancyTransfer?>
{
    public Task<AccountancyTransfer?> HandleAsync(GetAccountancyTransferQuery request, CancellationToken ct) => repository.GetTransferAsync(request.AccountancyId, ct);
}
