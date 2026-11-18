using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.SaveSupplyVendorMapping;

public sealed record SaveSupplyVendorMappingCommand(SupplyFinancialRequest Input) : ICommand<SupplyResult>;

public sealed class SaveSupplyVendorMappingCommandHandler(ISupplyFinancialIntegration integration) : ICommandHandler<SaveSupplyVendorMappingCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SaveSupplyVendorMappingCommand request, CancellationToken cancellationToken) =>
        integration.SaveVendorAsync(request.Input, cancellationToken);
}
