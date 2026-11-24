using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using System.Text.Json;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;

public sealed record SaveTaxPreviewCommand(Guid FactorId, string ActorId) : ICommand<Guid>;

public sealed class SaveTaxPreviewCommandHandler(ITaxationRepository repository) : ICommandHandler<SaveTaxPreviewCommand, Guid>
{
    public async Task<Guid> HandleAsync(SaveTaxPreviewCommand request, CancellationToken ct)
    {
        var source = await repository.GetPreviewSourceAsync(request.FactorId, ct);
        var preview = InvoicePreview.Build(source);
        if (preview.Errors.Count != 0 || preview.MemoryId is null)
            throw new ArgumentException("ابتدا خطاهای پیش‌نمایش را برطرف کنید.");
        return await repository.SaveSnapshotAsync(request.FactorId, preview.MemoryId.Value,
            JsonSerializer.Serialize(preview), request.ActorId, ct);
    }
}
