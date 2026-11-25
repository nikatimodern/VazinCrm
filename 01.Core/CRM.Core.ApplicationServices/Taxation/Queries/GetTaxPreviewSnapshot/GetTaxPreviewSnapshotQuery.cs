using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;
using System.Text.Json;

namespace CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPreviewSnapshot;

public sealed record GetTaxPreviewSnapshotQuery(Guid Id) : IQuery<InvoicePreview>;

public sealed class GetTaxPreviewSnapshotQueryHandler(ITaxationRepository repository)
    : IQueryHandler<GetTaxPreviewSnapshotQuery, InvoicePreview>
{
    public async Task<InvoicePreview> HandleAsync(GetTaxPreviewSnapshotQuery request, CancellationToken ct)
    {
        var json = await repository.ReadSnapshotAsync(request.Id, ct);
        return JsonSerializer.Deserialize<InvoicePreview>(json) ?? throw new JsonException();
    }
}
