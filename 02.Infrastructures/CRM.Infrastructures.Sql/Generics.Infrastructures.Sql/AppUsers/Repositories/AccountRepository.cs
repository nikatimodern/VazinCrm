#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Repositories;
using Microsoft.EntityFrameworkCore;
namespace CRM.Infrastructures.Sql.AppUsers.Repositories;

public sealed class AccountRepository(CRMContextDb db) : IAccountRepository
{
    public Task<List<AccountCustomer>> GetCustomersByMobileAsync(string? mobile, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().Where(c => c.Mobile == mobile)
            .Select(c => new AccountCustomer(c.Id, c.CompanyName, c.AgentName, c.RoleTypeTxt,c.RoleType)).ToListAsync(cancellationToken);
    public Task<AccountCustomer?> FindCustomerByMobileAsync(string mobile, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().Where(c => c.Mobile == mobile)
            .Select(c => new AccountCustomer(c.Id, c.CompanyName, c.AgentName, c.RoleTypeTxt,c.RoleType )).FirstOrDefaultAsync(cancellationToken);
    public async Task ReassignChatsAsync(string mobile, string userId, CancellationToken cancellationToken)
    {
        var chats = await db.MainChats.Where(c => c.ToUserId == mobile).ToListAsync(cancellationToken);
        foreach (var chat in chats) chat.ToUserId = userId;
    }
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
