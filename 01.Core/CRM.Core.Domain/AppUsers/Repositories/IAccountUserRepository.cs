#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
namespace CRM.Core.Domain.AppUsers.Repositories;

public sealed record AccountIdentityError(string Code, string Description);
public sealed record AccountIdentityResult(bool Succeeded, List<AccountIdentityError> Errors);
public interface IAccountUserRepository
{
    Task<AppUser?> FindByIdAsync(string? id, CancellationToken cancellationToken);
    Task<AppUser?> FindByNameAsync(string name, CancellationToken cancellationToken);
    Task<List<AppUser>> FindByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task<List<AppUser>> FindByUserNameAsync(string name, CancellationToken cancellationToken);
    Task<List<AppUser>> GetEmployeesAsync(CancellationToken cancellationToken);
    Task<List<AppUser>> GetInRoleAsync(string role, CancellationToken cancellationToken);
    Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken);
    Task<AccountIdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken);
    Task AddToRoleAsync(AppUser user, string role, CancellationToken cancellationToken);
    Task<AccountIdentityResult> ResetPasswordAsync(AppUser user, string password, CancellationToken cancellationToken);
    Task<AccountIdentityResult> UpdateLockoutAsync(AppUser user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
