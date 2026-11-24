using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.ListTaxMemories;

public sealed record ListTaxMemoriesQuery() : IQuery<IReadOnlyList<TaxMemory>>;

public sealed class ListTaxMemoriesQueryHandler(ITaxationRepository repository) : IQueryHandler<ListTaxMemoriesQuery, IReadOnlyList<TaxMemory>>
{
    public async Task<IReadOnlyList<TaxMemory>> HandleAsync(ListTaxMemoriesQuery request, CancellationToken ct)
    {
        return await repository.ListMemoriesAsync(ct);
    }
}
