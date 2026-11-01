using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.Accounts.Dtos;
namespace CRM.Core.ApplicationServices.Accounts.Abstractions;

public interface IAccountRequestContext
{
    string? UserId { get; }
    string? RemoteIp { get; }
    string UserAgent { get; }
    string? GetSession(string key);
    void SetSession(string key, string value);
    void ClearSession();
}
public sealed record AccountSignInResult(bool Succeeded, bool IsLockedOut);
public interface IAccountAuthentication
{
    Task<AccountSignInResult> PasswordSignInAsync(AppUser user, string password, CancellationToken cancellationToken);
    Task SignInAsync(AppUser user, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
}
public enum AccountAuditAction { Login, Logout, Created, PasswordReset }
public interface IAccountAudit { Task WriteAsync(AppUser user, AccountAuditAction action, string? details, CancellationToken cancellationToken); }
public interface IAccountOtpDelivery { Task SendAsync(string mobile, string code, bool forgot, CancellationToken cancellationToken); }
public interface IAccountImages { AccountImageResult Save(AccountUpload upload, string userId, string? oldUrl); }
public interface IAccountNotifications { Task AccessDeniedAsync(string userId, CancellationToken cancellationToken); }
