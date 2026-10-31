using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Customers.Abstractions;
using CRM.Core.ApplicationServices.Customers.Dtos;
using CRM.Core.ApplicationServices.Customers.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Repositories;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
namespace CRM.Core.ApplicationServices.Customers.Commands.NormalizeCustomers;

public sealed record NormalizeCustomersCommand() : ICommand<int>;
public sealed class NormalizeCustomersCommandHandler(ICustomerRepository repository, ICustomerEffects effects) : ICommandHandler<NormalizeCustomersCommand, int>
{
    public async Task<int> HandleAsync(NormalizeCustomersCommand request, CancellationToken ct)
    {
        var customers = await repository.GetForNormalizationAsync(ct);
        foreach (var customer in customers) { customer.NormalizedAgentName = effects.Normalize(customer.AgentName); customer.NormalizedCompanyName = effects.Normalize(customer.CompanyName); }
        await repository.SaveChangesAsync(ct);
        return customers.Count;
    }
}
