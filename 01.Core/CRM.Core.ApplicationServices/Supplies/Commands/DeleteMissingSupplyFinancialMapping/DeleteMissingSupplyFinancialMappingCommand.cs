using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.DeleteMissingSupplyFinancialMapping;

public sealed record DeleteMissingSupplyFinancialMappingCommand(Guid SourceId, Guid SoftwareId, Guid MappingId, bool Receipt)
    : ICommand<SupplyResult>;

public sealed class DeleteMissingSupplyFinancialMappingCommandHandler(ISupplyFinancialIntegration integration)
    : ICommandHandler<DeleteMissingSupplyFinancialMappingCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(DeleteMissingSupplyFinancialMappingCommand request, CancellationToken cancellationToken) =>
        integration.DeleteMissingMappingAsync(request.SourceId, request.SoftwareId, request.MappingId, request.Receipt, cancellationToken);
}
