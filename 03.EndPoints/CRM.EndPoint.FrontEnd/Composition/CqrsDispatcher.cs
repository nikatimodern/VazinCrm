using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;

public sealed class CqrsDispatcher(IServiceProvider services) : ICqrsDispatcher
{
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var dispatch = DispatchCache<TResult>.Queries.GetOrAdd(query.GetType(), static requestType =>
            QueryMethod.MakeGenericMethod(requestType, typeof(TResult))
                .CreateDelegate<Func<IServiceProvider, IQuery<TResult>, CancellationToken, Task<TResult>>>());
        return dispatch(services, query, cancellationToken);
    }

    public Task<TResult> CommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var dispatch = DispatchCache<TResult>.Commands.GetOrAdd(command.GetType(), static requestType =>
            CommandMethod.MakeGenericMethod(requestType, typeof(TResult))
                .CreateDelegate<Func<IServiceProvider, ICommand<TResult>, CancellationToken, Task<TResult>>>());
        return dispatch(services, command, cancellationToken);
    }

    // Cache typed delegates, never handlers or scoped service providers.
    private static readonly MethodInfo QueryMethod = typeof(CqrsDispatcher)
        .GetMethod(nameof(DispatchQuery), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo CommandMethod = typeof(CqrsDispatcher)
        .GetMethod(nameof(DispatchCommand), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Task<TResult> DispatchQuery<TQuery, TResult>(
        IServiceProvider services, IQuery<TResult> query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResult> =>
        services.GetRequiredService<IQueryHandler<TQuery, TResult>>().HandleAsync((TQuery)query, cancellationToken);

    private static Task<TResult> DispatchCommand<TCommand, TResult>(
        IServiceProvider services, ICommand<TResult> command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResult> =>
        services.GetRequiredService<ICommandHandler<TCommand, TResult>>().HandleAsync((TCommand)command, cancellationToken);

    private static class DispatchCache<TResult>
    {
        public static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IQuery<TResult>, CancellationToken, Task<TResult>>> Queries = new();
        public static readonly ConcurrentDictionary<Type, Func<IServiceProvider, ICommand<TResult>, CancellationToken, Task<TResult>>> Commands = new();
    }
}
