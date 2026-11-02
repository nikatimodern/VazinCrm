using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Repositories;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.AddAccountancyTransfer;

public sealed record AddAccountancyTransferCommand(AccountancyTransfer Model) : ICommand<AccountancyOutcome>;

public sealed class AddAccountancyTransferCommandHandler(IAccountancyRepository repository) : ICommandHandler<AddAccountancyTransferCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(AddAccountancyTransferCommand request, CancellationToken ct)
    {
        if (request.Model == null || request.Model.AccountancyId == Guid.Empty)
            return new(400);
        await repository.SaveTransferAsync(request.Model, ct);
        return new(200);
    }
}
