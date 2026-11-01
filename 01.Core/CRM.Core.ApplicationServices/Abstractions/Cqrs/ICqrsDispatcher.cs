namespace CRM.Core.ApplicationServices.Abstractions.Cqrs;

public interface ICqrsDispatcher
{
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    Task<TResult> CommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
