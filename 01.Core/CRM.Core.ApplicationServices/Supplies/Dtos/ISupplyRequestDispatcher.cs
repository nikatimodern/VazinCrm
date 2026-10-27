using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Dtos;
// Dispatches the module's existing CQRS contracts; implementations belong to the composition root.
public interface ISupplyRequestDispatcher
{
    Task<SupplyResult> QueryAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<SupplyResult>;
    Task<SupplyResult> CommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<SupplyResult>;
}
