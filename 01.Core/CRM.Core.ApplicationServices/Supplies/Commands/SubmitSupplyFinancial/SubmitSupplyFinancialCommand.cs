using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Commands.SubmitSupplyFinancial;

public sealed record SubmitSupplyFinancialCommand(SupplyFinancialRequest Input) : ICommand<SupplyResult>;

public sealed class SubmitSupplyFinancialCommandHandler(ISupplyFinancialIntegration integration) : ICommandHandler<SubmitSupplyFinancialCommand, SupplyResult>
{
    public Task<SupplyResult> HandleAsync(SubmitSupplyFinancialCommand request, CancellationToken cancellationToken) =>
        integration.SubmitAsync(request.Input, cancellationToken);
}
