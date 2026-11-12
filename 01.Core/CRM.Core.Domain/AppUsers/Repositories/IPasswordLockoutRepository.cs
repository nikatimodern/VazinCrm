using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.Core.Domain.AppUsers.Repositories;

public interface IPasswordLockoutRepository
{
    Task<PasswordAttemptResult?> GetStatusAsync(string userId, CancellationToken cancellationToken);
    Task<PasswordAttemptResult> RegisterAttemptAsync(
        string userId, bool passwordIsValid, CancellationToken cancellationToken);
}
