using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.Core.Domain.AppUsers.Repositories;

public interface IOtpMobileRepository
{
    Task<OtpActionResult> IssueAsync(string mobile, string type, string code, CancellationToken cancellationToken);
    Task<OtpActionResult> VerifyAsync(string mobile, string type, string? code, CancellationToken cancellationToken);
    Task<OtpActionResult?> GetStatusAsync(string mobile, string type, CancellationToken cancellationToken);
    Task CancelAsync(string mobile, string type, string code, CancellationToken cancellationToken);
    Task ResetLockoutAsync(string mobile, CancellationToken cancellationToken);
}
