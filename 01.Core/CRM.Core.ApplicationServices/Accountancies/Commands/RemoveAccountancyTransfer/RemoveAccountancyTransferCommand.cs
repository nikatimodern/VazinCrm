using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Repositories;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.RemoveAccountancyTransfer;

public sealed record RemoveAccountancyTransferCommand(Guid AccountancyId) : ICommand<AccountancyOutcome>;

public sealed class RemoveAccountancyTransferCommandHandler(IAccountancyRepository repository) : ICommandHandler<RemoveAccountancyTransferCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(RemoveAccountancyTransferCommand request, CancellationToken ct)
        => await repository.RemoveTransferAsync(request.AccountancyId, ct) ? new(200) : new(404);
}
