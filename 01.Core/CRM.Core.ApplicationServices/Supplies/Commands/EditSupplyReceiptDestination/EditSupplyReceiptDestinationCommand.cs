using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.EditSupplyReceiptDestination;

public sealed record EditSupplyReceiptDestinationCommand(SupplyReceiptEditRequest Edit) : ICommand<SupplyResult>;

public sealed class EditSupplyReceiptDestinationCommandHandler(ISupplyFinancialIntegration integration) : ICommandHandler<EditSupplyReceiptDestinationCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(EditSupplyReceiptDestinationCommand request, CancellationToken cancellationToken) =>
        integration.EditReceiptAsync(request.Edit, cancellationToken);
}
