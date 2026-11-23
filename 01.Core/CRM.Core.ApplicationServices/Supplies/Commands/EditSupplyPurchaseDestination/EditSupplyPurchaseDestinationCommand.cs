using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.EditSupplyPurchaseDestination;

public sealed record EditSupplyPurchaseDestinationCommand(SupplyDestinationEditRequest Edit) : ICommand<SupplyResult>;

public sealed class EditSupplyPurchaseDestinationCommandHandler(ISupplyFinancialIntegration integration) : ICommandHandler<EditSupplyPurchaseDestinationCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(EditSupplyPurchaseDestinationCommand request, CancellationToken cancellationToken) =>
        integration.EditPurchaseAsync(request.Edit, cancellationToken);
}
