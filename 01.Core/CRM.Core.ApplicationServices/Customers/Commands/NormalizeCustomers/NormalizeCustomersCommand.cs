using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Customers.Abstractions;
using CRM.Core.Domain.Customers.Repositories;

namespace CRM.Core.ApplicationServices.Customers.Commands.NormalizeCustomers;

public sealed record NormalizeCustomersCommand() : ICommand<int>;

public sealed class NormalizeCustomersCommandHandler(
    ICustomerRepository repository, ICustomerEffects effects)
    : ICommandHandler<NormalizeCustomersCommand, int>
{
    public async Task<int> HandleAsync(NormalizeCustomersCommand request, CancellationToken ct)
    {
        // Search-index normalization is a shared maintenance action and does
        // not require a per-entity permission or a special Identity role.
        var customers = await repository.GetForNormalizationAsync(ct);
        foreach (var customer in customers)
        {
            customer.NormalizedAgentName = effects.Normalize(customer.AgentName);
            customer.NormalizedCompanyName = effects.Normalize(customer.CompanyName);
        }
        await repository.SaveChangesAsync(ct);
        return customers.Count;
    }
}
