using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Customers.Repositories;

namespace CRM.Core.ApplicationServices.Accountancies.Commands.AddAccountancyTransfer;

public sealed record AddAccountancyTransferCommand(AccountancyTransfer Model) : ICommand<AccountancyOutcome>;

public sealed class AddAccountancyTransferCommandHandler(IAccountancyRepository repository, ICustomerRepository customers) : ICommandHandler<AddAccountancyTransferCommand, AccountancyOutcome>
{
    public async Task<AccountancyOutcome> HandleAsync(AddAccountancyTransferCommand request, CancellationToken ct)
    {
        if (request.Model == null || request.Model.AccountancyId == Guid.Empty)
            return new(400);
        if (request.Model.CustomerId is { } customerId)
        {
            var customer = await customers.GetAsync(customerId, false, false, ct);
            if (customer == null) return new(400);
            request.Model.Beneficiary = string.Join(" - ", new[] { customer.CompanyName, customer.AgentName, customer.CustomerCode.ToString() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }
        await repository.SaveTransferAsync(request.Model, ct);
        return new(200);
    }
}
