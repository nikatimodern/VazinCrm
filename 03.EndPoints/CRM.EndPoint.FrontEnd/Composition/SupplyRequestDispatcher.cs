using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Supplies.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;
public sealed class SupplyRequestDispatcher(IServiceProvider services) : ISupplyRequestDispatcher
{
    public Task<SupplyResult> QueryAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<SupplyResult> => services.GetRequiredService<IQueryHandler<TQuery, SupplyResult>>().HandleAsync(query, cancellationToken);
    public Task<SupplyResult> CommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<SupplyResult> => services.GetRequiredService<ICommandHandler<TCommand, SupplyResult>>().HandleAsync(command, cancellationToken);
}
