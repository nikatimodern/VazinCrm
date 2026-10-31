using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;

namespace CRM.Core.ApplicationServices.Transfers.Queries.GetTransfersTable;
public sealed record GetTransfersTableQuery(TransferTableRequest Filter) : IQuery<TransferTableResult>;
public sealed class GetTransfersTableQueryHandler(ITransferRepository repository, ITransferEffects effects) : IQueryHandler<GetTransfersTableQuery, TransferTableResult>
{
    public async Task<TransferTableResult> HandleAsync(GetTransfersTableQuery request, CancellationToken ct)
    {
        var filter = request.Filter;
        return await repository.GetTableAsync(filter with { SearchNumber = effects.ParseSearchNumber(filter.Search) }, ct);
    }
}
