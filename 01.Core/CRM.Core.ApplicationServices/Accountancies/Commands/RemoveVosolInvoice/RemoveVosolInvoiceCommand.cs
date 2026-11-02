using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Repositories;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.RemoveVosolInvoice;

public sealed record RemoveVosolInvoiceCommand(Guid AccountancyId) : ICommand<AccountancyOutcome>;

public sealed class RemoveVosolInvoiceCommandHandler(IAccountancyRepository repository) : ICommandHandler<RemoveVosolInvoiceCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(RemoveVosolInvoiceCommand request, CancellationToken ct)
        => await repository.RemoveVosolAsync(request.AccountancyId, ct) ? new(200) : new(404);
}
