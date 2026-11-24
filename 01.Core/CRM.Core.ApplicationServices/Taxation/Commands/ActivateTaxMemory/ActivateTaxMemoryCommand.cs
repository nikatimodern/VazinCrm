using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.ActivateTaxMemory;

public sealed record ActivateTaxMemoryCommand(Guid Id) : ICommand<bool>;

public sealed class ActivateTaxMemoryCommandHandler(ITaxationRepository repository) : ICommandHandler<ActivateTaxMemoryCommand, bool>
{
    public async Task<bool> HandleAsync(ActivateTaxMemoryCommand request, CancellationToken ct)
    {
        await repository.ActivateAsync(request.Id, ct);
        return true;
    }
}
