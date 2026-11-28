using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.SynchronizeSupplyDestination;

public sealed record SynchronizeSupplyDestinationCommand(SupplyDestinationSyncRequest Input) : ICommand<SupplyResult>;

public sealed class SynchronizeSupplyDestinationCommandHandler(ISupplyFinancialIntegration integration)
    : ICommandHandler<SynchronizeSupplyDestinationCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SynchronizeSupplyDestinationCommand command, CancellationToken ct) =>
        integration.SynchronizeAsync(command.Input, ct);
}
