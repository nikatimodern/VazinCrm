using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.CheckTaxV1Connection;

public interface ITaxV1IdentityProbe
{
    Task CheckAsync(string fiscalId, TaxSigningBinding binding, CancellationToken ct);
}

public sealed record CheckTaxV1ConnectionQuery(Guid MemoryId) : IQuery<bool>;

public sealed class CheckTaxV1ConnectionQueryHandler(ITaxationRepository repository, ITaxSigningBindings bindings,
    ITaxV1IdentityProbe probe) : IQueryHandler<CheckTaxV1ConnectionQuery, bool>
{
    public async Task<bool> HandleAsync(CheckTaxV1ConnectionQuery request, CancellationToken ct)
    {
        var memories = await repository.ListMemoriesAsync(ct);
        var memory = memories.SingleOrDefault(x => x.Id == request.MemoryId) ?? throw new KeyNotFoundException();
        var binding = await bindings.GetAsync(memory.Id, ct);
        if (binding.Protocol != TaxAuthorityProtocol.V1)
            throw new ArgumentException("روش فعال شرکت نسخه ۱ نیست.");
        await probe.CheckAsync(memory.FiscalId, binding, ct);
        return true;
    }
}
