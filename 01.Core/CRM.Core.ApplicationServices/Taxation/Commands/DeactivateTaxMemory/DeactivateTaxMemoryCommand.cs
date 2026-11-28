using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.DeactivateTaxMemory;

public sealed record DeactivateTaxMemoryCommand(Guid Id) : ICommand<bool>;

public sealed class DeactivateTaxMemoryCommandHandler(ITaxationRepository repository) : ICommandHandler<DeactivateTaxMemoryCommand, bool>
{
    public async Task<bool> HandleAsync(DeactivateTaxMemoryCommand request, CancellationToken ct)
    {
        await repository.DeactivateAsync(request.Id, ct);
        return true;
    }
}
