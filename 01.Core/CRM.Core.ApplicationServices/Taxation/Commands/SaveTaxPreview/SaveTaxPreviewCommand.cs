using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;
using System.Text.Json;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;

public sealed record SaveTaxPreviewCommand(Guid FactorId, string ActorId, string Version) : ICommand<Guid>;

public sealed class SaveTaxPreviewCommandHandler(ITaxationRepository repository) : ICommandHandler<SaveTaxPreviewCommand, Guid>
{
    public Task<Guid> HandleAsync(SaveTaxPreviewCommand request, CancellationToken ct) =>
        repository.SaveSnapshotAsync(request.FactorId, request.ActorId,
            source => SerializeValidated(source, request.Version), ct);

    internal static string SerializeValidated(TaxPreviewSource source, string version)
    {
        var preview = InvoicePreview.Build(source);
        if (string.IsNullOrWhiteSpace(version) || !string.Equals(version, preview.Version, StringComparison.Ordinal))
            throw new TaxPreviewConflictException();
        if (preview.Errors.Count != 0 || preview.MemoryId is null)
            throw new ArgumentException("ابتدا خطاهای پیش‌نمایش را برطرف کنید.");
        return JsonSerializer.Serialize(preview);
    }
}
