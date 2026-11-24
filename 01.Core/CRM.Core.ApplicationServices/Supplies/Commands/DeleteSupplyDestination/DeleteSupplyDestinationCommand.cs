using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.DeleteSupplyDestination;

public sealed record DeleteSupplyDestinationCommand(SupplyDestinationKey Key, string Token) : ICommand<SupplyResult>;

public sealed class DeleteSupplyDestinationCommandHandler(ISupplyFinancialIntegration integration) : ICommandHandler<DeleteSupplyDestinationCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(DeleteSupplyDestinationCommand request, CancellationToken cancellationToken) =>
        integration.DeleteDestinationAsync(request.Key, request.Token, cancellationToken);
}
