using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetAccountHome;

public sealed record GetAccountHomeQuery() : IQuery<GetAccountHomeResult>;

public sealed class GetAccountHomeQueryHandler(IAccountUserRepository users, IAccountRepository repository, IAccountRequestContext context) : IQueryHandler<GetAccountHomeQuery, GetAccountHomeResult>
{
    public async Task<GetAccountHomeResult> HandleAsync(GetAccountHomeQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken);
        if (user is null) return new(new("OtpLogin"), []);
        if (user.IsEmployee) return new(new("Index", "Dashboard"), []);
        var customers = await repository.GetCustomersByMobileAsync(user.PhoneNumber, cancellationToken);
        return new(null, customers.Select(c => new AccountCustomerOption(c.Id, string.IsNullOrWhiteSpace(c.CompanyName) ? $"{c.AgentName}" : $"{c.CompanyName} ({c.AgentName})")).ToList());
    }
}
public sealed record AccountCustomerOption(Guid CustomerId, string CustomerName);
public sealed record GetAccountHomeResult(AccountRedirect? Redirect, List<AccountCustomerOption> Customers);
