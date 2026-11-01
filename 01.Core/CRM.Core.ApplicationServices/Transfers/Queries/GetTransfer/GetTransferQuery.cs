using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;

namespace CRM.Core.ApplicationServices.Transfers.Queries.GetTransfer;
public sealed record GetTransferQuery(Guid Id) : IQuery<TransferDetails?>;
public sealed class GetTransferQueryHandler(ITransferRepository repository) : IQueryHandler<GetTransferQuery, TransferDetails?>
{
    public async Task<TransferDetails?> HandleAsync(GetTransferQuery request, CancellationToken ct)
    {
        return await repository.GetDetailsAsync(request.Id, ct);
    }
}
