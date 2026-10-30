#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace CRM.Core.Domain.AppUsers.Repositories;

public sealed record AccountCustomer(Guid Id, string? CompanyName, string? AgentName, string? RoleTypeTxt);
public interface IAccountRepository
{
    Task<List<AccountCustomer>> GetCustomersByMobileAsync(string? mobile, CancellationToken cancellationToken);
    Task<AccountCustomer?> FindCustomerByMobileAsync(string mobile, CancellationToken cancellationToken);
    Task ReassignChatsAsync(string mobile, string userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
