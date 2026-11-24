using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoice;

public sealed record EnqueueTaxInvoiceCommand(Guid FactorId, string ActorId, string Version) : ICommand<Guid>;

public sealed class EnqueueTaxInvoiceCommandHandler(ITaxationRepository repository) : ICommandHandler<EnqueueTaxInvoiceCommand, Guid>
{
    public Task<Guid> HandleAsync(EnqueueTaxInvoiceCommand request, CancellationToken ct) =>
        repository.EnqueueAsync(request.FactorId, request.ActorId,
            source => SaveTaxPreviewCommandHandler.SerializeValidated(source, request.Version), ct);
}
