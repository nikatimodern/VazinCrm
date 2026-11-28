using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;
using System.Text.Json.Serialization;

namespace CRM.Core.ApplicationServices.Taxation.Queries.GetTaxConnectionCheck;

public sealed record TaxConnectionStep(string Name, string Status, string Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TaxConnectionDiagnostic>? Diagnostics { get; init; }
}

public sealed record TaxConnectionDiagnostic(string Url, IReadOnlyList<string> HeaderNames, string RequestJson,
    int? HttpStatusCode, string? ContentType, string? ErrorCode, string? ErrorDescription);
public interface ITaxConnectionCheck
{
    Task<IReadOnlyList<TaxConnectionStep>> CheckAsync(TaxMemory memory, CancellationToken ct);
}
public sealed record GetTaxConnectionCheckQuery(Guid MemoryId) : IQuery<IReadOnlyList<TaxConnectionStep>>;
public sealed class GetTaxConnectionCheckQueryHandler(ITaxMemorySettingsRepository repository, ITaxConnectionCheck checker)
    : IQueryHandler<GetTaxConnectionCheckQuery, IReadOnlyList<TaxConnectionStep>>
{
    public async Task<IReadOnlyList<TaxConnectionStep>> HandleAsync(GetTaxConnectionCheckQuery request, CancellationToken ct) =>
        await checker.CheckAsync(await repository.FindMemoryAsync(request.MemoryId, ct) ?? throw new KeyNotFoundException(), ct);
}
