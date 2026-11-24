using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;
using System.Text.Json;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;

public sealed record SaveTaxPreviewCommand(Guid FactorId, string ActorId, string Version, ExportTaxDetails? Export = null) : ICommand<Guid>;

public sealed class SaveTaxPreviewCommandHandler(ITaxationRepository repository) : ICommandHandler<SaveTaxPreviewCommand, Guid>
{
    public Task<Guid> HandleAsync(SaveTaxPreviewCommand request, CancellationToken ct) =>
        repository.SaveSnapshotAsync(request.FactorId, request.ActorId,
            source => SerializeValidated(source, request.Version, request.Export), ct);

    internal static string SerializeValidated(TaxPreviewSource source, string version, ExportTaxDetails? export = null)
    {
        var preview = InvoicePreview.Build(source, export: export);
        if (string.IsNullOrWhiteSpace(version) || !string.Equals(version, preview.Version, StringComparison.Ordinal))
            throw new TaxPreviewConflictException();
        return SerializeReady(preview);
    }

    internal static string SerializeReady(InvoicePreview preview)
    {
        if (preview.Errors.Count != 0 || preview.MemoryId is null)
            throw new ArgumentException("ابتدا خطاهای پیش‌نمایش را برطرف کنید.");
        return JsonSerializer.Serialize(preview);
    }
}
